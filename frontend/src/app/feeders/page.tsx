"use client";

import { Box } from "@mui/material";
import StorageOutlinedIcon from "@mui/icons-material/StorageOutlined";
import { PageHeader } from "@/components/PageHeader";
import { ReportColumn } from "@/components/reports/ReportTable";
import { MasterDataListView } from "@/components/reports/MasterDataListView";

interface FeederRow {
  id: string;
  code: string;
  name: string;
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
  dtrCount: number;
}

const columns: ReportColumn<FeederRow>[] = [
  { key: "code", label: "Feeder Code", render: (r) => r.code },
  { key: "name", label: "Feeder Name", render: (r) => r.name },
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
  { key: "dtrCount", label: "DTR Count", align: "right", render: (r) => r.dtrCount },
];

export default function FeedersPage() {
  return (
    <Box>
      <PageHeader icon={<StorageOutlinedIcon fontSize="small" />} title="Feeder" />
      <MasterDataListView<FeederRow>
        title="Feeder Master Data"
        endpoint="/api/v1/network/feeders"
        filenamePrefix="MDMS_Feeders"
        columns={columns}
        rowKey={(r) => r.id}
        searchPlaceholder="Feeder code or name"
      />
    </Box>
  );
}
