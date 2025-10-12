import React, { useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import {
  fetchSeatMap,
  SeatSelection,
  postQuote,
  postBooking,
  QuoteRequestPayload,
  OrderQuote
} from '@/hooks/useApi';
import {
  Alert,
  Badge,
  Box,
  Button,
  Card,
  Divider,
  Grid,
  Group,
  List,
  Loader,
  Paper,
  Stack,
  Text,
  Title,
  Tooltip
} from '@mantine/core';
import { IconChairDirector, IconInfoCircle, IconReceipt, IconTicket, IconUser } from '@tabler/icons-react';

const SeatSelectionPage = () => {
  const { screeningId } = useParams<{ screeningId: string }>();
  const [selectedSeats, setSelectedSeats] = useState<SeatSelection[]>([]);

  const { data: seatMap, isLoading } = useQuery({
    queryKey: ['seatmap', screeningId],
    queryFn: () => fetchSeatMap(screeningId!),
    enabled: Boolean(screeningId)
  });

  const quoteMutation = useMutation({
    mutationFn: (payload: QuoteRequestPayload) => postQuote(payload)
  });

  const bookingMutation = useMutation({
    mutationFn: postBooking
  });

  const toggleSeat = (seatLabel: string) => {
    setSelectedSeats((prev) => {
      const exists = prev.some((s) => s.seatLabel === seatLabel);
      if (exists) return prev.filter((s) => s.seatLabel !== seatLabel);
      return [...prev, { seatLabel, ticketTypeId: 't_adult' }];
    });
  };

  const requestQuote = () => {
    if (!screeningId || selectedSeats.length === 0) return;
    quoteMutation.mutate({ screeningId, seats: selectedSeats });
  };

  const confirmBooking = () => {
    if (!screeningId || selectedSeats.length === 0) return;
    bookingMutation.mutate({
      screeningId,
      customerName: 'Guest',
      customerEmail: 'guest@example.com',
      customerPhone: '000',
      promoCode: '',
      seats: selectedSeats,
      createAccount: false,
      password: ''
    });
  };

  if (isLoading) {
    return (
      <Card withBorder padding="xl" radius="lg" style={{ background: '#202020', borderColor: '#2f2f2f' }}>
        <Group gap="sm">
          <Loader size="sm" color="tealAccent" />
          <Text size="sm" c="gray.4">
            Loading seat map…
          </Text>
        </Group>
      </Card>
    );
  }

  if (!seatMap) {
    return (
      <Alert color="red" variant="light" icon={<IconInfoCircle size={18} />}>
        Unable to load seat map for this screening.
      </Alert>
    );
  }

  return (
    <Grid gutter="xl">
      <Grid.Col span={{ base: 12, lg: 8 }}>
        <Card withBorder padding="xl" radius="lg" style={{ background: '#202020', borderColor: '#2f2f2f' }}>
          <Stack gap="md">
            <Group gap="sm">
              <IconChairDirector size={20} />
              <div>
                <Title order={3}>Select seats</Title>
                <Text size="sm" c="gray.4">
                  Tap to select available seats. Reserved seats are locked by other guests.
                </Text>
              </div>
            </Group>
            <Divider color="rgba(255,255,255,0.05)" />
            <SeatGrid seatMap={seatMap} selected={selectedSeats} onToggle={toggleSeat} />
            <Legend />
          </Stack>
        </Card>
      </Grid.Col>
      <Grid.Col span={{ base: 12, lg: 4 }}>
        <Stack gap="lg">
          <Card withBorder radius="lg" padding="lg" style={{ background: '#202020', borderColor: '#2f2f2f' }}>
            <Stack gap="md">
              <Group gap="sm">
                <IconTicket size={18} />
                <Title order={4}>Selection summary</Title>
              </Group>
              {selectedSeats.length === 0 ? (
                <Text size="sm" c="gray.4">
                  Choose seats from the map to begin your booking.
                </Text>
              ) : (
                <Group gap="xs">
                  {selectedSeats.map((seat) => (
                    <Badge key={seat.seatLabel} color="tealAccent" variant="light">
                      {seat.seatLabel}
                    </Badge>
                  ))}
                </Group>
              )}

              <Group gap="sm">
                <Button
                  onClick={requestQuote}
                  disabled={selectedSeats.length === 0 || quoteMutation.isPending}
                  color="tealAccent"
                  leftSection={<IconReceipt size={16} />}
                >
                  {quoteMutation.isPending ? 'Pricing…' : 'Price selection'}
                </Button>
                <Tooltip label="Demo placeholder – replace with checkout flow">
                  <Button
                    variant="outline"
                    color="copperAccent"
                    onClick={confirmBooking}
                    disabled={selectedSeats.length === 0 || bookingMutation.isPending}
                  >
                    Quick book
                  </Button>
                </Tooltip>
              </Group>
            </Stack>
          </Card>

          {quoteMutation.data && <QuotePanel quote={quoteMutation.data} />}

          {bookingMutation.isSuccess && (
            <Alert color="teal" variant="light" icon={<IconUser size={18} />}>
              Demo booking captured! Replace with real checkout in the final flow.
            </Alert>
          )}
        </Stack>
      </Grid.Col>
    </Grid>
  );
};

const SeatGrid = ({
  seatMap,
  selected,
  onToggle
}: {
  seatMap: Awaited<ReturnType<typeof fetchSeatMap>>;
  selected: SeatSelection[];
  onToggle: (seat: string) => void;
}) => {
  const selectedLabels = useMemo(() => new Set(selected.map((s) => s.seatLabel)), [selected]);

  const rows = Array.from({ length: seatMap.rows }, (_, i) => String.fromCharCode(65 + i));
  const cols = Array.from({ length: seatMap.columns }, (_, i) => i + 1);

  return (
    <Box ta="center">
      <Paper
        radius="md"
        withBorder
        mb="md"
        style={{
          display: 'inline-block',
          padding: '1.5rem',
          background: '#181818',
          borderColor: '#2f2f2f'
        }}
      >
        <Text size="xs" c="gray.4" mb="xs" tt="uppercase" fw={600}>
          Front of cinema
        </Text>
        <Box
          style={{
            display: 'grid',
            gap: '0.4rem',
            gridTemplateColumns: `40px repeat(${cols.length}, 42px)`
          }}
        >
          <span />
          {cols.map((col) => (
            <Text key={col} size="xs" c="gray.5">
              {col}
            </Text>
          ))}
          {rows.map((row) => (
            <React.Fragment key={row}>
              <Text size="xs" c="gray.5" style={{ lineHeight: '42px' }}>
                {row}
              </Text>
              {cols.map((col) => {
                const seatLabel = `${row}${col}`;
                const state = seatMap.seats[seatLabel] ?? 'Available';
                const isSelected = selectedLabels.has(seatLabel);

                return (
                  <SeatButton
                    key={seatLabel}
                    seatLabel={seatLabel}
                    state={state}
                    isSelected={isSelected}
                    onToggle={onToggle}
                  />
                );
              })}
            </React.Fragment>
          ))}
        </Box>
      </Paper>
    </Box>
  );
};

const SeatButton = ({
  seatLabel,
  state,
  isSelected,
  onToggle
}: {
  seatLabel: string;
  state: string;
  isSelected: boolean;
  onToggle: (seat: string) => void;
}) => {
  const palette = getSeatPalette(state, isSelected);
  const disabled = state === 'Booked' || state === 'Blocked';

  return (
    <Tooltip label={disabled ? `${seatLabel} reserved` : `${seatLabel} available`}>
      <Button
        variant={isSelected ? 'filled' : 'subtle'}
        color={palette.color}
        onClick={() => onToggle(seatLabel)}
        disabled={disabled}
        style={{
          width: 40,
          height: 40,
          padding: 0,
          borderRadius: 10
        }}
      >
        <Text size="xs">{seatLabel}</Text>
      </Button>
    </Tooltip>
  );
};

const Legend = () => (
  <Group gap="lg" justify="center">
    <LegendItem color="tealAccent" label="Selected" />
    <LegendItem color="gray" label="Available" variant="outline" />
    <LegendItem color="dark" label="Reserved" disabled />
  </Group>
);

const LegendItem = ({
  color,
  label,
  variant = 'light',
  disabled = false
}: {
  color: string;
  label: string;
  variant?: 'light' | 'outline';
  disabled?: boolean;
}) => (
  <Group gap={6}>
    <Badge color={color} variant={variant} radius="sm">
      <Box style={{ width: 12, height: 12 }} />
    </Badge>
    <Text size="xs" c={disabled ? 'gray.5' : 'gray.3'}>
      {label}
    </Text>
  </Group>
);

const getSeatPalette = (state: string, isSelected: boolean) => {
  if (isSelected) return { color: 'tealAccent' };
  switch (state) {
    case 'Booked':
      return { color: 'dark' };
    case 'Blocked':
      return { color: 'gray' };
    default:
      return { color: 'gray' };
  }
};

const QuotePanel = ({ quote }: { quote: OrderQuote }) => (
  <Card withBorder radius="lg" padding="lg" style={{ background: '#202020', borderColor: '#2f2f2f' }}>
    <Stack gap="md">
      <Group gap="sm">
        <IconReceipt size={18} />
        <Title order={5}>Order summary</Title>
      </Group>
      <List spacing="xs" size="sm">
        {quote.lines.map((line) => (
          <List.Item key={`${line.description}-${line.lineTotal}`}>
            <Group justify="space-between">
              <span>{line.description}</span>
              <span>${line.lineTotal.toFixed(2)}</span>
            </Group>
          </List.Item>
        ))}
      </List>
      <Divider color="rgba(255,255,255,0.05)" />
      <Stack gap={4} align="stretch">
        <Group justify="space-between" c="gray.5" size="sm">
          <span>Subtotal</span>
          <span>${quote.subtotal.toFixed(2)}</span>
        </Group>
        {quote.discount !== 0 && (
          <Group justify="space-between" c="gray.5" size="sm">
            <span>Discounts</span>
            <span>${quote.discount.toFixed(2)}</span>
          </Group>
        )}
        <Group justify="space-between" fw={600}>
          <span>Total</span>
          <span>${quote.total.toFixed(2)}</span>
        </Group>
      </Stack>
    </Stack>
  </Card>
);

export default SeatSelectionPage;
