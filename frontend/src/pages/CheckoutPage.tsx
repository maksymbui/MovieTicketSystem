import React, { useMemo, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import {
  Alert,
  Button,
  Card,
  Divider,
  Grid,
  Group,
  Stack,
  Text,
  TextInput,
  Title
} from '@mantine/core';
import { IconArrowLeft, IconCheck, IconInfoCircle } from '@tabler/icons-react';
import { postBooking, OrderQuote, ScreeningSummary, SeatSelection, TicketType } from '@/hooks/useApi';
import { summariseQuote, totalFromSummary } from '@/utils/ticketing';

type CheckoutLocationState = {
  screeningId: string;
  seatAssignments: SeatSelection[];
  seatLabels: string[];
  quote: OrderQuote;
  ticketTypes: TicketType[];
  session?: ScreeningSummary;
};

const CheckoutPage = () => {
  const { screeningId: routeScreeningId } = useParams<{ screeningId: string }>();
  const location = useLocation();
  const navigate = useNavigate();
  const state = location.state as CheckoutLocationState | undefined;

  const screeningId = routeScreeningId ?? state?.screeningId;
  const seatAssignments = state?.seatAssignments ?? [];
  const seatLabels = state?.seatLabels ?? [];
  const quote = state?.quote;
  const ticketTypes = state?.ticketTypes ?? [];
  const session = state?.session;

  const [customerName, setCustomerName] = useState('');
  const [customerEmail, setCustomerEmail] = useState('');
  const [customerPhone, setCustomerPhone] = useState('');

  const bookingMutation = useMutation({
    mutationFn: postBooking
  });

  const summaryLines = useMemo(() => summariseQuote(quote, seatAssignments, ticketTypes), [quote, seatAssignments, ticketTypes]);
  const total = quote?.total ?? totalFromSummary(summaryLines);

  const formatCurrency = (value: number) =>
    new Intl.NumberFormat(undefined, { style: 'currency', currency: 'AUD' }).format(value);

  const sessionTime = session ? new Date(session.startUtc) : undefined;
  const formattedSessionTime = sessionTime
    ? new Intl.DateTimeFormat(undefined, {
        weekday: 'short',
        day: 'numeric',
        month: 'short',
        hour: 'numeric',
        minute: '2-digit'
      }).format(sessionTime)
    : undefined;

  const canCheckout =
    Boolean(screeningId) &&
    seatAssignments.length > 0 &&
    quote &&
    customerName.trim().length > 0 &&
    customerEmail.trim().length > 0 &&
    customerPhone.trim().length > 0 &&
    !bookingMutation.isPending;

  const handleCheckout = () => {
    if (!screeningId || !quote) return;
    bookingMutation.mutate({
      screeningId,
      customerName,
      customerEmail,
      customerPhone,
      promoCode: '',
      seats: seatAssignments,
      createAccount: false,
      password: ''
    });
  };

  if (!state || !screeningId || !quote || seatAssignments.length === 0) {
    return (
      <Alert color="orange" variant="light" icon={<IconInfoCircle size={18} />}>
        Checkout details were not found. Please select your seats again.
        <Button mt="md" variant="light" color="tealAccent" onClick={() => navigate('/movies')}>
          Back to catalog
        </Button>
      </Alert>
    );
  }

  return (
    <Grid gutter="xl">
      <Grid.Col span={{ base: 12, lg: 8 }}>
        <Card withBorder radius="lg" padding="xl" style={{ background: '#202020', borderColor: '#2f2f2f' }}>
          <Stack gap="lg">
            <Group justify="space-between">
              <Group gap="sm">
                <IconCheck size={20} />
                <Title order={3}>Review &amp; checkout</Title>
              </Group>
              <Button
                variant="subtle"
                color="gray"
                leftSection={<IconArrowLeft size={16} />}
                onClick={() => navigate(-1)}
              >
                Adjust selection
              </Button>
            </Group>

            <Stack gap="xs">
              {session?.movie && (
                <Text fw={600} size="lg">
                  {session.movie.title}
                </Text>
              )}
              {session && (
                <Text size="sm" c="gray.4">
                  {session.cinemaName} • {formattedSessionTime} • {session.class}
                </Text>
              )}
              <Text size="sm" c="gray.4">
                Seats: {seatLabels.join(', ')}
              </Text>
            </Stack>

            <Divider color="rgba(255,255,255,0.05)" />

            <Stack gap="sm">
              <Title order={5}>Ticket summary</Title>
              {summaryLines.map((line) => (
                <Group key={line.ticketType.id} justify="space-between">
                  <Text size="sm">
                    {line.count} × {line.ticketType.name}
                  </Text>
                  <Text size="sm">{formatCurrency(line.total)}</Text>
                </Group>
              ))}
              <Divider color="rgba(255,255,255,0.05)" />
              <Group justify="space-between" c="gray.5">
                <Text size="sm">Subtotal</Text>
                <Text size="sm">{formatCurrency(quote.subtotal)}</Text>
              </Group>
              {console.log('DEBUG: quote.discount =', quote.discount)}
              {(quote.discount !== 0 || true) && (
                <Group justify="space-between" c="gray.5">
                  <Text size="sm">Discounts</Text>
                  <Text size="sm">{formatCurrency(quote.discount || 0)}</Text>
                </Group>
              )}
              <Group justify="space-between" fw={600}>
                <Text>Total</Text>
                <Text>{formatCurrency(total)}</Text>
              </Group>
            </Stack>

            <Divider color="rgba(255,255,255,0.05)" />

            <Stack gap="sm">
              <Title order={5}>Contact details</Title>
              <Text size="sm" c="gray.4">
                Tickets will be emailed to the address provided below.
              </Text>
              <TextInput
                label="Full name"
                placeholder="Jane Smith"
                value={customerName}
                onChange={(event) => setCustomerName(event.currentTarget.value)}
                required
              />
              <TextInput
                label="Email"
                placeholder="jane@example.com"
                value={customerEmail}
                onChange={(event) => setCustomerEmail(event.currentTarget.value)}
                required
              />
              <TextInput
                label="Phone number"
                placeholder="0400 000 000"
                value={customerPhone}
                onChange={(event) => setCustomerPhone(event.currentTarget.value)}
                required
              />
            </Stack>

            {bookingMutation.isError && (
              <Alert color="red" variant="light" icon={<IconInfoCircle size={18} />}>
                {bookingMutation.error instanceof Error
                  ? bookingMutation.error.message
                  : 'Unable to complete booking. Please try again.'}
              </Alert>
            )}

            {bookingMutation.isSuccess ? (
              <Alert color="teal" variant="light" icon={<IconCheck size={18} />}>
                <Stack gap="xs">
                  <span>
                    Booking confirmed! Reference {bookingMutation.data.referenceCode}. A confirmation email
                    has been sent to {bookingMutation.data.customerEmail}.
                  </span>
                  <Button
                    size="xs"
                    variant="light"
                    color="tealAccent"
                    onClick={() => navigate('/movies')}
                  >
                    Back to catalog
                  </Button>
                </Stack>
              </Alert>
            ) : (
              <Group justify="flex-end">
                <Button
                  color="tealAccent"
                  onClick={handleCheckout}
                  disabled={!canCheckout}
                >
                  {bookingMutation.isPending ? 'Processing…' : 'Checkout'}
                </Button>
              </Group>
            )}
          </Stack>
        </Card>
      </Grid.Col>
    </Grid>
  );
};

export default CheckoutPage;
