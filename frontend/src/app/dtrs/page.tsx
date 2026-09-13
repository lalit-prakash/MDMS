"use client";

import { Box } from "@mui/material";
import ElectricMeterOutlinedIcon from "@mui/icons-material/ElectricMeterOutlined";
import { PageHeader } from "@/components/PageHeader";
import { ReportColumn } from "@/components/reports/ReportTable";
import { MasterDataListView } from "@/components/reports/MasterDataListView";

interface DtrRow {
  id: string;
  code: string;
  name: string;
  feederCode: string;
  feederName: string;
  substationCode: string;
  substationName: string;
  zone: string | null;
  circle: string | null;
  division: string | null;
  subDivision: string | null;
  section: string | null;
  capacityKva: number | null;
  voltageLevel: string | null;
  make: string | null;
  commissionedOn: string | null;
  operationalStatus: string | null;
  consumerCount: number;
}

const columns: ReportColumn<DtrRow>[] = [
  { key: "code", label: "DTR Code", render: (r) => r.code },
  { key: "name", label: "DTR Name", render: (r) => r.name },
  { key: "feederCode", label: "Feeder", render: (r) => r.feederCode },
  { key: "substationCode", label: "Substation", render: (r) => r.substationCode },
  { key: "zone", label: "Zone", render: (r) => r.zone ?? "—" },
  { key: "circle", label: "Circle", render: (r) => r.circle ?? "—" },
  { key: "division", label: "Division", render: (r) => r.division ?? "—" },
  { key: "subDivision", label: "Sub Division", render: (r) => r.subDivision ?? "—" },
  { key: "section", label: "Section", render: (r) => r.section ?? "—" },
  { key: "capacityKva", label: "Capacity (kVA)", align: "right", render: (r) => r.capacityKva ?? "—" },
  { key: "voltageLevel", label: "Voltage Level", render: (r) => r.voltageLevel ?? "—" },
  { key: "make", label: "Make", render: (r) => r.make ?? "—" },
  { key: "commissionedOn", label: "Commissioned On", render: (r) => r.commissionedOn ?? "—" },
  { key: "operationalStatus", label: "Status", render: (r) => r.operationalStatus ?? "—" },
  { key: "consumerCount", label: "Consumer Count", align: "right", render: (r) => r.consumerCount },
];

export default function DtrsPage() {
  return (
    <Box>
      <PageHeader icon={<ElectricMeterOutlinedIcon fontSize="small" />} title="DTR" />
      <MasterDataListView<DtrRow>
        title="DTR Master Data"
        endpoint="/api/v1/network/dtrs"
        filenamePrefix="MDMS_DTRs"
        columns={columns}
        rowKey={(r) => r.id}
        searchPlaceholder="DTR code or name"
      />
    </Box>
  );
}
