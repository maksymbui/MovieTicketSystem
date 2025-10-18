import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  TextInput,
  Alert,
  ActionIcon,
  Button,
  Card,
  Divider,
  Grid,
  Group,
  Loader,
  Stack,
  Text,
  Title,
  Tooltip
} from '@mantine/core';
import { IconChairDirector, IconInfoCircle, IconMinus, IconPlus, IconTicket } from '@tabler/icons-react';
import {
  fetchSeatMap,
  fetchTicketTypes,
  postBooking,
  postQuote,
  ScreeningSummary,
  SeatSelection,
  TicketType
} from '@/hooks/useApi';
import {
  TicketCounts,
  buildSeatAssignments,
  clampTicketCountsToTarget,
  compareSeatLabels,
  summariseQuote,
  totalFromSummary
} from '@/utils/ticketing';
import classes from './SeatSelectionPage.module.css';

const SeatSelectionPage = () => {
  const { screeningId } = useParams<{ screeningId: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const session = (location.state as { session?: ScreeningSummary } | undefined)?.session;

  const [selectedSeatLabels, setSelectedSeatLabels] = useState<string[]>([]);
  const [ticketCounts, setTicketCounts] = useState<TicketCounts>({});
  const [promoCode, setPromoCode] = useState('');
  const [appliedPromo, setAppliedPromo] = useState('');

  const { data: seatMap, isLoading: seatMapLoading } = useQuery({
    queryKey: ['seatmap', screeningId],
    queryFn: () => fetchSeatMap(screeningId!),
    enabled: Boolean(screeningId)
  });

  const { data: ticketTypes = [], isLoading: ticketTypesLoading } = useQuery({
    queryKey: ['ticket-types'],
    queryFn: fetchTicketTypes
  });

  const defaultTicketTypeId = useMemo(() => {
    if (!ticketTypes.length) return undefined;
    const preferred = ticketTypes.find((type) => type.category === 'Adult');
    return (preferred ?? ticketTypes[0])?.id;
  }, [ticketTypes]);

  const toggleSeat = useCallback(
    (seatLabel: string) => {
      setSelectedSeatLabels((prev) => {
        if (prev.includes(seatLabel)) {
          const next = prev.filter((label) => label !== seatLabel);
          setTicketCounts((current) =>
            next.length === 0 ? {} : clampTicketCountsToTarget(current, ticketTypes, next.length)
          );
          return next;
        }

        const next = [...prev, seatLabel].sort(compareSeatLabels);
        return next;
      });
    },
    [ticketTypes]
  );

  useEffect(() => {
    if (selectedSeatLabels.length === 0 && Object.keys(ticketCounts).length > 0) {
      setTicketCounts({});
    }
  }, [selectedSeatLabels.length, ticketCounts]);

  const selectedSeatCount = selectedSeatLabels.length;
  const ticketsSelected = useMemo(
    () => Object.values(ticketCounts).reduce((sum, value) => sum + value, 0),
    [ticketCounts]
  );

  const seatAssignments = useMemo(
    () => buildSeatAssignments(selectedSeatLabels, ticketCounts, ticketTypes),
    [selectedSeatLabels, ticketCounts, ticketTypes]
  );

  const seatAssignmentsKey = useMemo(
    () => seatAssignments.map((assignment) => `${assignment.seatLabel}:${assignment.ticketTypeId}`).join('|'),
    [seatAssignments]
  );

  const quoteEnabled =
    Boolean(screeningId) &&
    selectedSeatCount > 0 &&
    seatAssignments.length === selectedSeatCount &&
    ticketsSelected === selectedSeatCount;

  const { data: quote, isFetching: quoteLoading, refetch: refetchQuote } = useQuery({
    queryKey: ['quote', screeningId, seatAssignmentsKey, appliedPromo],
    queryFn: () => postQuote({ screeningId: screeningId!, seats: seatAssignments, promoCode: appliedPromo }),
    enabled: quoteEnabled
  });

  const summaryLines = useMemo(
    () => summariseQuote(quote, seatAssignments, ticketTypes),
    [quote, seatAssignments, ticketTypes]
  );

  const summaryDeal = quote?.discount ?? 0;
  const summarySubtotal = quote?.subtotal ?? totalFromSummary(summaryLines);
  const summaryTotal = quote?.total ?? summarySubtotal;
  const seatRemainder = Math.max(selectedSeatCount - ticketsSelected, 0);
  const proceedDisabled = !quoteEnabled || !quote || quoteLoading;

  const bookingMutation = useMutation({
    mutationFn: postBooking,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['seatmap', screeningId] });
      setSelectedSeatLabels([]);
      setTicketCounts({});
    }
  });

  const formatCurrency = useCallback(
    (value: number) =>
      new Intl.NumberFormat(undefined, { style: 'currency', currency: 'AUD' }).format(value),
    []
  );

  const handleIncrement = (typeId: string) => {
    if (selectedSeatCount === 0 || ticketsSelected >= selectedSeatCount) return;
    setTicketCounts((prev) => ({ ...prev, [typeId]: (prev[typeId] ?? 0) + 1 }));
  };

  const handleDecrement = (typeId: string) => {
    setTicketCounts((prev) => {
      const current = prev[typeId] ?? 0;
      if (current <= 0) return prev;
      const nextValue = current - 1;
      const next = { ...prev };
      if (nextValue <= 0) delete next[typeId];
      else next[typeId] = nextValue;
      return next;
    });
  };

  const handleQuickBook = () => {
    if (!screeningId || selectedSeatLabels.length === 0 || !defaultTicketTypeId) return;

    const quickCounts: TicketCounts = { [defaultTicketTypeId]: selectedSeatLabels.length };
    const quickAssignments = buildSeatAssignments(selectedSeatLabels, quickCounts, ticketTypes);
    setTicketCounts(quickCounts);

    bookingMutation.mutate({
      screeningId,
      customerName: 'Guest',
      customerEmail: 'guest@example.com',
      customerPhone: '0000000000',
      promoCode: '',
      seats: quickAssignments,
      createAccount: false,
      password: ''
    });
  };

  const handleProceed = () => {
    if (!screeningId || !quote || seatAssignments.length !== selectedSeatCount) return;
    navigate(`/screenings/${screeningId}/checkout`, {
      state: {
        screeningId,
        seatAssignments,
        seatLabels: selectedSeatLabels,
        quote,
        ticketTypes,
        session,
        promoCode: appliedPromo
      }
    });
  };

  if (seatMapLoading) {
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
            <SeatGrid seatMap={seatMap} selectedLabels={selectedSeatLabels} onToggle={toggleSeat} />
            <Legend />
          </Stack>
        </Card>
      </Grid.Col>
      <Grid.Col span={{ base: 12, lg: 4 }}>
        <Stack gap="lg">
          <Card withBorder radius="lg" padding="lg" style={{ background: '#202020', borderColor: '#2f2f2f' }}>
            <Stack gap="md">
              <Group gap="sm" align="flex-start">
                <IconTicket size={18} />
                <div>
                  <Title order={4}>Ticket selection</Title>
                  <Text size="sm" c="gray.4">
                    Tickets {ticketsSelected}/{selectedSeatCount}
                  </Text>
                </div>
              </Group>

              {selectedSeatCount === 0 && (
                <Text size="sm" c="gray.4">
                  Select seats on the left to assign ticket types.
                </Text>
              )}

              {ticketTypesLoading && (
                <Group gap="sm">
                  <Loader size="xs" color="tealAccent" />
                  <Text size="sm" c="gray.4">
                    Loading ticket types…
                  </Text>
                </Group>
              )}

              {!ticketTypesLoading &&
                ticketTypes.map((type) => {
                  const count = ticketCounts[type.id] ?? 0;
                  return (
                    <Group key={type.id} justify="space-between" align="center">
                      <div>
                        <Text fw={500}>{type.name}</Text>
                        <Text size="xs" c="gray.5">
                          {formatCurrency(type.price)}
                        </Text>
                      </div>
                      <Group gap="xs" align="center">
                        <ActionIcon
                          variant="light"
                          color="gray"
                          radius="xl"
                          size="sm"
                          onClick={() => handleDecrement(type.id)}
                          disabled={count === 0}
                        >
                          <IconMinus size={14} />
                        </ActionIcon>
                        <Text fw={600} w={20} ta="center">
                          {count}
                        </Text>
                        <ActionIcon
                          variant="light"
                          color="tealAccent"
                          radius="xl"
                          size="sm"
                          onClick={() => handleIncrement(type.id)}
                          disabled={selectedSeatCount === 0 || ticketsSelected >= selectedSeatCount}
                        >
                          <IconPlus size={14} />
                        </ActionIcon>
                      </Group>
                    </Group>
                  );
                })}

              {seatRemainder > 0 && selectedSeatCount > 0 && (
                <Text size="xs" c="copperAccent.4">
                  Assign {seatRemainder} more ticket{seatRemainder === 1 ? '' : 's'} to match your seats.
                </Text>
              )}

              <Divider color="rgba(255,255,255,0.05)" />

              <Stack gap={4}>
                {summaryLines.length > 0 ? (
                  summaryLines.map((line) => (
                    <Group key={line.ticketType.id} justify="space-between" gap="xs">
                      <Text size="sm">
                        {line.count} × {line.ticketType.name}
                      </Text>
                      <Text size="sm">{formatCurrency(line.total)}</Text>
                    </Group>
                  ))
                ) : (
                  <Text size="sm" c="gray.5">
                    Assign at least one ticket type to preview pricing.
                  </Text>
                )}
              </Stack>

              <Divider color="rgba(255,255,255,0.05)" />

              <Group gap="sm" align="center">
                <TextInput
                  value={promoCode}
                  onChange={e => setPromoCode(e.currentTarget.value)}
                  placeholder="Enter promo code"
                  size="xs"
                  style={{ width: '140px' }}
                  disabled={quoteLoading}
                />
                <Button
                  size="xs"
                  variant="outline"
                  color="tealAccent"
                  onClick={() => {
                    setAppliedPromo(promoCode);
                    refetchQuote();
                  }}
                  disabled={quoteLoading || !promoCode.trim()}
                >
                  Apply
                </Button>
              </Group>
              <Group justify="space-between" c="gray.5">
                <Text size="sm">Subtotal</Text>
                <Text size="sm">{formatCurrency(summarySubtotal)}</Text>
              </Group>
              <Group justify="space-between" c="gray.5">
                <Text size="sm">Deal</Text>
                <Text size="sm">{formatCurrency(summaryDeal)}</Text>
              </Group>
              <Group justify="space-between" fw={600}>
                <Text>Total</Text>
                <Text>{formatCurrency(summaryTotal)}</Text>
              </Group>

              <Button
                color="tealAccent"
                radius="md"
                onClick={handleProceed}
                disabled={proceedDisabled}
              >
                {quoteLoading ? 'Preparing…' : 'Proceed to review'}
              </Button>

              <Tooltip label="Auto-fill all seats as Adult and complete a demo booking." withinPortal>
                <Button
                  variant="outline"
                  color="copperAccent"
                  radius="md"
                  onClick={handleQuickBook}
                  disabled={
                    bookingMutation.isPending ||
                    selectedSeatLabels.length === 0 ||
                    !defaultTicketTypeId
                  }
                >
                  Quick book
                </Button>
              </Tooltip>

              {bookingMutation.isSuccess && bookingMutation.data && (
                <Alert color="teal" variant="light" icon={<IconTicket size={18} />}>
                  Booking captured! Reference {bookingMutation.data.referenceCode}. Seats have been
                  locked.
                </Alert>
              )}

              {bookingMutation.isError && (
                <Alert color="red" variant="light" icon={<IconInfoCircle size={18} />}>
                  {bookingMutation.error instanceof Error
                    ? bookingMutation.error.message
                    : 'Unable to create booking.'}
                </Alert>
              )}
            </Stack>
          </Card>
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
  selectedLabels,
  onToggle
}: {
  seatMap: Awaited<ReturnType<typeof fetchSeatMap>>;
  selectedLabels: string[];
  onToggle: (seat: string) => void;
}) => {
  const selected = useMemo(() => new Set(selectedLabels), [selectedLabels]);
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
                  const isSelected = selected.has(seatLabel);
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
  if (state === 'Booked') classNames.push(classes.reserved);
  if (state === 'Blocked') classNames.push(classes.blocked);

  return (
    <Tooltip label={disabled ? `${seatLabel} reserved` : `${seatLabel} available`} openDelay={200}>
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

export default SeatSelectionPage;
