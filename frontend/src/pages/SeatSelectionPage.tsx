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
import { Alert, Badge, Button, Card, Divider, Grid, Group, List, Loader, Stack, Text, Title, Tooltip } from '@mantine/core';
import { IconChairDirector, IconInfoCircle, IconReceipt, IconTicket, IconUser } from '@tabler/icons-react';
import classes from './SeatSelectionPage.module.css';

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

const computeAisleBreaks = (columns: number) => {
  if (columns <= 6) return [];
  const breaks: number[] = [];
  for (let i = 4; i < columns; i += 4) {
    if (i < columns) breaks.push(i);
  }
  return breaks;
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
  const rows = useMemo(() => Array.from({ length: seatMap.rows }, (_, i) => String.fromCharCode(65 + i)), [seatMap.rows]);
  const columns = useMemo(() => Array.from({ length: seatMap.columns }, (_, i) => i + 1), [seatMap.columns]);
  const aisleBreaks = useMemo(() => computeAisleBreaks(seatMap.columns), [seatMap.columns]);

  return (
    <div className={classes.mapOuter}>
      <div className={classes.screenHeader}>Front of cinema</div>
      <div className={classes.mapScroll}>
        <div className={classes.mapContent}>
          {rows.map((row, rowIndex) => (
            <div className={classes.row} key={row}>
              <span className={classes.rowLabel}>{row}</span>
              <div className={classes.seatRow}>
                {columns.flatMap((col) => {
                  const seatLabel = `${row}${col}`;
                  const state = seatMap.seats[seatLabel] ?? 'Available';
                  const isSelected = selectedLabels.has(seatLabel);
                  const seatNode = (
                    <SeatButton
                      key={seatLabel}
                      seatLabel={seatLabel}
                      state={state}
                      isSelected={isSelected}
                      onToggle={onToggle}
                    />
                  );

                  if (aisleBreaks.includes(col) && col !== seatMap.columns) {
                    return [seatNode, <div key={`${row}-${col}-aisle`} className={classes.aisle} />];
                  }

                  return [seatNode];
                })}
              </div>
            </div>
          ))}

          <div className={classes.footer}>
            <span className={classes.footerSpacer} />
            <div className={classes.columnNumbers}>
              {columns.flatMap((col) => {
                const label = (
                  <span key={`col-${col}`} className={classes.columnNumber}>
                    {col}
                  </span>
                );
                if (aisleBreaks.includes(col) && col !== seatMap.columns) {
                  return [label, <div key={`col-${col}-aisle`} className={classes.aisle} />];
                }
                return [label];
              })}
            </div>
          </div>
        </div>
      </div>
    </div>
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
  const disabled = state === 'Booked' || state === 'Blocked';
  const classNames = [classes.seat];
  if (isSelected) classNames.push(classes.selected);
  else if (state === 'Booked') classNames.push(classes.reserved);
  else if (state === 'Blocked') classNames.push(classes.blocked);

  return (
    <Tooltip label={disabled ? `${seatLabel} reserved` : `${seatLabel} available`} openDelay={250}>
      <button
        type="button"
        className={classNames.join(' ')}
        onClick={() => onToggle(seatLabel)}
        disabled={disabled}
        aria-label={disabled ? `${seatLabel} reserved` : `${seatLabel} available`}
      />
    </Tooltip>
  );
};

const Legend = () => (
  <div className={classes.legend}>
    <LegendItem label="Selected" swatchClass={`${classes.legendSwatch} ${classes.legendSwatchSelected}`} />
    <LegendItem label="Available" swatchClass={`${classes.legendSwatch} ${classes.legendSwatchAvailable}`} />
    <LegendItem label="Reserved" swatchClass={`${classes.legendSwatch} ${classes.legendSwatchReserved}`} />
  </div>
);

const LegendItem = ({ label, swatchClass }: { label: string; swatchClass: string }) => (
  <div className={classes.legendItem}>
    <span className={swatchClass} />
    <span>{label}</span>
  </div>
);

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
