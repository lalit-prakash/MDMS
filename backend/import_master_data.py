"""
Imports the utility's three master-info Excel exports (Feeder, DTR, Consumer) through the real
HTTP API, so every business rule runs as it would for a live caller. Idempotent: re-running skips
anything already present (matched by code / RR number / meter serial) and never overwrites.

  Region > Zone > Circle > Division > SubDivision   -> OrgUnit tree (get-or-create by parent+name)
  Sub Station / Feeder / DTR                        -> HierarchyNode tree, substation linked to its SubDivision
  Consumer rows                                     -> Customer (account no. = RR Number) + ServicePoint
                                                       + Meter (MSN) installed at that service point

Usage:  python import_master_data.py [folder-with-the-3-xlsx-files]      (default: ~/Downloads)
Requires: backend on http://localhost:5004, Admin login lalit.prakash / Test@123, openpyxl.
"""

import json
import re
import sys
import urllib.error
import urllib.request
from datetime import date, datetime, timezone
from pathlib import Path

import openpyxl

BASE = "http://localhost:5004"
TOKEN = None
FILES = {
    "feeder": "Feeder Master info.xlsx",
    "dtr": "DTR master info.xlsx",
    "consumer": "Consumer master info.xlsx",
}


def call(method, path, body=None):
    req = urllib.request.Request(BASE + path, data=json.dumps(body, default=str).encode() if body is not None else None, method=method)
    req.add_header("Content-Type", "application/json")
    if TOKEN:
        req.add_header("Authorization", f"Bearer {TOKEN}")
    try:
        with urllib.request.urlopen(req) as r:
            raw = r.read()
            return r.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode(errors="replace")


def must(method, path, body=None):
    status, payload = call(method, path, body)
    if status not in (200, 201):
        raise RuntimeError(f"{method} {path} -> {status}: {payload}")
    return payload


# ------------------------------------------------------------------------------ cell parsing
def s(v):
    if v is None:
        return None
    t = str(v).strip()
    return t or None


def num(v):
    try:
        return float(v) if v is not None and str(v).strip() != "" else None
    except ValueError:
        return None


def integer(v):
    n = num(v)
    return int(n) if n is not None else None


def parse_dt(v):
    if v is None:
        return None
    if isinstance(v, datetime):
        return v
    for fmt in ("%d-%b-%Y %H:%M:%S", "%d-%b-%Y", "%d-%m-%Y"):
        try:
            return datetime.strptime(str(v).strip(), fmt)
        except ValueError:
            pass
    return None


def iso(dt):
    return dt.replace(tzinfo=timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ") if dt else None


def d_only(dt):
    return dt.date().isoformat() if dt else None


def yes_no(v):
    t = (s(v) or "").upper()
    return True if t in ("YES", "Y", "TRUE") else False if t in ("NO", "N", "FALSE") else None


def read_sheet(path):
    ws = openpyxl.load_workbook(path, read_only=True, data_only=True).active
    rows = list(ws.iter_rows(values_only=True))
    hi = next(i for i, r in enumerate(rows) if sum(c is not None for c in r) > 8)
    header = [s(h) for h in rows[hi]]
    out = []
    for r in rows[hi + 1:]:
        if all(c is None for c in r):
            continue
        out.append({h: v for h, v in zip(header, r) if h})
    return out


def substation_code(name):
    """'BATAMARI 18093002 ' -> '18093002'; a name with no trailing token stays as-is (code max 32)."""
    parts = (s(name) or "").split()
    return (parts[-1] if parts and re.search(r"\d", parts[-1]) else " ".join(parts))[:32]


# ------------------------------------------------------------------------------ importer state
class Importer:
    def __init__(self):
        self.org = {}  # (unitType, parentId, name) -> id
        self.nodes = {}  # (nodeType, code) -> node dict
        self.customers = {}  # accountNumber -> customer
        self.meters = {}  # serial -> meter
        self.stats = {"org": 0, "substation": 0, "feeder": 0, "dtr": 0, "consumer": 0, "meter": 0, "skipped": 0}

    def load_existing(self):
        for u in must("GET", "/api/v1/config/org-units"):
            self.org[(u["unitType"], u["parentId"], u["name"])] = u["id"]
        for n in must("GET", "/api/v1/config/hierarchy"):
            self.nodes[(n["nodeType"], n["code"])] = n
        for c in must("GET", "/api/v1/customers"):
            self.customers[c["accountNumber"]] = c
        for m in must("GET", "/api/v1/meters"):
            self.meters[m["serialNumber"]] = m

    def org_chain(self, row):
        parent = None
        for unit_type, key in (("Region", "Region"), ("Zone", "Zone"), ("Circle", "Circle"), ("Division", "Division"), ("SubDivision", "Subdivision")):
            name = s(row.get(key))
            if not name:
                continue
            k = (unit_type, parent, name)
            if k not in self.org:
                code = f"{unit_type[:3].upper()}-{re.sub('[^A-Z0-9]+', '_', name.upper())}"[:32]
                self.org[k] = must("POST", "/api/v1/config/org-units", {"unitType": unit_type, "parentId": parent, "code": code, "name": name})["id"]
                self.stats["org"] += 1
            parent = self.org[k]
        return parent  # finest unit available

    def node(self, node_type, code, name, parent_id):
        k = (node_type, code)
        if k in self.nodes:
            return self.nodes[k], False
        n = must("POST", "/api/v1/config/hierarchy", {"nodeType": node_type, "parentId": parent_id, "code": code[:32], "name": name[:256]})
        self.nodes[k] = n
        return n, True

    def substation(self, row, code_key=None):
        name = s(row.get("SubStation Name")) or s(row.get("Sub Station")) or "UNKNOWN"
        code = s(row.get(code_key)) if code_key else None
        code = code or substation_code(name)
        sub, created = self.node("Substation", code, name, None)
        if created:
            self.stats["substation"] += 1
            unit = self.org_chain(row)
            if unit:
                must("POST", f"/api/v1/config/hierarchy/{sub['id']}/org-unit", {"orgUnitId": unit})
        return sub

    def feeder(self, row, code, name, substation):
        f, created = self.node("Feeder", code, name or code, substation["id"])
        if created:
            self.stats["feeder"] += 1
        return f, created

    def dtr(self, code, name, feeder):
        d, created = self.node("DistributionTransformer", code, name or code, feeder["id"])
        if created:
            self.stats["dtr"] += 1
        return d, created


def import_feeders(imp, rows):
    for r in rows:
        code = s(r.get("Feeder Code"))
        if not code:
            continue
        sub = imp.substation(r, "SS Code")
        f, created = imp.feeder(r, code, s(r.get("Feeder Name")), sub)
        if not created:
            imp.stats["skipped"] += 1
            continue
        inst = parse_dt(r.get("Installation Date"))
        must("POST", f"/api/v1/config/hierarchy/{f['id']}/master-data", {
            "voltageLevel": s(r.get("Voltage Level")), "make": s(r.get("Make")), "commissionedOn": d_only(inst),
            "latitude": num(r.get("Latitude")), "longitude": num(r.get("Longitude")),
        })
        must("POST", f"/api/v1/config/hierarchy/{f['id']}/asset-data", {
            "meterSerial": s(r.get("MSN")), "meterMake": s(r.get("Make")), "multiplyingFactor": num(r.get("MF")),
            "externalCtRatio": s(r.get("External CT Ratio")), "externalPtRatio": s(r.get("External PT Ratio")),
            "mect": num(r.get("MECT")), "mept": num(r.get("MEPT")), "feederMode": s(r.get("Mode")),
            "installedBy": s(r.get("Installed By")), "satno": integer(r.get("Satno")), "mdmAssetTimestampUtc": iso(parse_dt(r.get("MDM Asset TimeStamp"))),
        })


def import_dtrs(imp, rows):
    for r in rows:
        code = s(r.get("DTR Code"))
        if not code:
            continue
        sub = imp.substation(r)
        fcode = s(r.get("Feeder Code")) or "UNKNOWN"
        f, _ = imp.feeder(r, fcode, s(r.get("Feeder")), sub)
        d, created = imp.dtr(code, s(r.get("DTR")), f)
        if not created:
            imp.stats["skipped"] += 1
            continue
        inst = parse_dt(r.get("Installation Date"))
        must("POST", f"/api/v1/config/hierarchy/{d['id']}/master-data", {
            "capacityKva": num(r.get("DTR Rating")), "make": s(r.get("MSN Make")), "commissionedOn": d_only(inst),
            "latitude": num(r.get("Latitude")), "longitude": num(r.get("Longitude")),
        })
        must("POST", f"/api/v1/config/hierarchy/{d['id']}/asset-data", {
            "meterSerial": s(r.get("MSN")), "meterMake": s(r.get("MSN Make")), "multiplyingFactor": num(r.get("MF")),
            "externalCtRatio": s(r.get("External CT Ratio")), "dtrType": s(r.get("DTR Type")),
            "installedBy": s(r.get("Installed By")), "satno": integer(r.get("Satno")), "mdmAssetTimestampUtc": iso(parse_dt(r.get("MDM Asset TimeStamp"))),
        })


def import_consumers(imp, rows):
    for r in rows:
        rr = s(r.get("RRNumber"))
        if not rr:
            continue
        if rr in imp.customers:
            imp.stats["skipped"] += 1
            continue

        sub = imp.substation(r)
        f, _ = imp.feeder(r, s(r.get("Feeder code")) or "UNKNOWN", s(r.get("Feeder")), sub)
        dtr_code = s(r.get("DTR Code"))
        dt = imp.dtr(dtr_code, s(r.get("DTR")), f)[0] if dtr_code else None

        cust = must("POST", "/api/v1/customers", {"accountNumber": rr, "name": s(r.get("Consumer Name")) or rr})
        imp.customers[rr] = cust
        sp = must("POST", f"/api/v1/customers/{cust['id']}/service-points", {
            "address": s(r.get("Address")) or "Address not provided", "distributionTransformerNodeId": dt["id"] if dt else None})

        must("POST", f"/api/v1/customers/{cust['id']}/master-data", {
            "rrNumber": rr, "mobileNumber": s(r.get("Mobile Number")), "connectionStatus": s(r.get("Connection Status")),
            "serviceDate": d_only(parse_dt(r.get("Service Date"))),
            "sanctionedLoadKw": num(r.get("Sanctioned Load")), "contractDemandKva": num(r.get("Contract_Demand")),
            "connectedLoadKw": num(r.get("CONNECTED_LOAD")), "loadType": s(r.get("Load Type")), "tariffCategoryCode": s(r.get("Tariff")),
            "communicationType": s(r.get("Communication")), "paymentMode": s(r.get("Payment Mode")), "isNetMeter": yes_no(r.get("NetMeter")),
            "billDay": integer(r.get("Bill Day")), "billCycle": s(r.get("Bill Cycle")),
            "latitude": num(r.get("Latitude")), "longitude": num(r.get("Longitude")),
        })
        must("POST", f"/api/v1/customers/{cust['id']}/meter-asset-data", {
            "meterMake": s(r.get("Make")), "meterPhase": s(r.get("Phase")), "multiplyingFactor": num(r.get("MF")),
            "isMrRequiredDone": yes_no(r.get("Is MR-Required Done")), "satno": integer(r.get("Satno")),
            "mdmAssetTimestampUtc": iso(parse_dt(r.get("MDM Asset TimeStamp"))),
            "meterReplacementDate": d_only(parse_dt(r.get("Meter Replacement Date"))),
        })
        imp.stats["consumer"] += 1

        msn = s(r.get("MSN"))
        if msn and msn not in imp.meters:
            phase = "Three" if (s(r.get("Phase")) or "").startswith("3") else "Single"
            m = must("POST", "/api/v1/meters", {"serialNumber": msn, "phase": phase})
            imp.meters[msn] = m
            installed = parse_dt(r.get("Service Date")) or datetime.now(timezone.utc)
            must("POST", f"/api/v1/meters/{m['id']}/install", {
                "servicePointId": sp["id"], "effectiveFromUtc": iso(installed), "openingReading": 0,
                "reason": "Imported from Consumer master info"})
            imp.stats["meter"] += 1


def main():
    global TOKEN
    folder = Path(sys.argv[1]) if len(sys.argv) > 1 else Path.home() / "Downloads"
    TOKEN = must("POST", "/api/v1/auth/login", {"username": "lalit.prakash", "password": "Test@123"})["accessToken"]

    imp = Importer()
    imp.load_existing()
    # Feeders first (they carry the SS code), then DTRs, then consumers, so each level reuses the last.
    import_feeders(imp, read_sheet(folder / FILES["feeder"]))
    print("feeders done", imp.stats)
    import_dtrs(imp, read_sheet(folder / FILES["dtr"]))
    print("dtrs done", imp.stats)
    import_consumers(imp, read_sheet(folder / FILES["consumer"]))
    print("consumers done", imp.stats)


if __name__ == "__main__":
    main()
