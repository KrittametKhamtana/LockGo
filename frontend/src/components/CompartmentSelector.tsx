import { Card, CardActionArea, CardContent, Chip, Stack, Typography } from "@mui/material";
import type { Compartment } from "../types/locker";

const SIZE_LABEL: Record<Compartment["size"], string> = {
  S: "Small",
  M: "Medium",
  L: "Large",
};

interface CompartmentSelectorProps {
  compartments: Compartment[];
  selectedId: string | null;
  onSelect: (compartment: Compartment) => void;
}

export function CompartmentSelector({ compartments, selectedId, onSelect }: CompartmentSelectorProps) {
  return (
    <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
      {compartments.map((compartment) => {
        const isAvailable = compartment.status === "Available";
        const isSelected = compartment.id === selectedId;

        return (
          <Card
            key={compartment.id}
            variant="outlined"
            sx={{
              flex: 1,
              borderColor: isSelected ? "primary.main" : undefined,
              borderWidth: isSelected ? 2 : 1,
              opacity: isAvailable ? 1 : 0.5,
            }}
          >
            <CardActionArea
              disabled={!isAvailable}
              onClick={() => onSelect(compartment)}
              sx={{ p: 1 }}
            >
              <CardContent>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Typography variant="subtitle1" fontWeight={700}>
                    {SIZE_LABEL[compartment.size]}
                  </Typography>
                  <Chip
                    size="small"
                    label={isAvailable ? "Available" : "Occupied"}
                    color={isAvailable ? "success" : "default"}
                  />
                </Stack>
                <Typography variant="h6" sx={{ mt: 1 }}>
                  ฿{compartment.price.toFixed(0)}
                </Typography>
              </CardContent>
            </CardActionArea>
          </Card>
        );
      })}
    </Stack>
  );
}
