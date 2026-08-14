import type { ReactNode } from "react";
import { Card, CardContent, Divider, Stack, Typography } from "@mui/material";

export interface SummaryRow {
  label: string;
  value: ReactNode;
}

interface SummaryCardProps {
  title: string;
  subtitle?: string;
  rows: SummaryRow[];
  footer?: ReactNode;
}

export function SummaryCard({ title, subtitle, rows, footer }: SummaryCardProps) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="h6">{title}</Typography>
        {subtitle && (
          <Typography variant="body2" color="text.secondary">
            {subtitle}
          </Typography>
        )}

        <Divider sx={{ my: 2 }} />

        <Stack spacing={1.25}>
          {rows.map((row) => (
            <Stack key={row.label} direction="row" justifyContent="space-between" gap={2}>
              <Typography variant="body2" color="text.secondary">
                {row.label}
              </Typography>
              <Typography variant="body2" fontWeight={600} sx={{ textAlign: "right" }}>
                {row.value}
              </Typography>
            </Stack>
          ))}
        </Stack>

        {footer && (
          <>
            <Divider sx={{ my: 2 }} />
            {footer}
          </>
        )}
      </CardContent>
    </Card>
  );
}
