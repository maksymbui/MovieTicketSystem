import axios from 'axios';

const baseURL = import.meta.env.DEV ? '/api' : 'http://localhost:5000/api';

const client = axios.create({
  baseURL
});

export const fetchMovies = async () => {
  const { data } = await client.get('/movies');
  return data as Movie[];
};

export const fetchScreenings = async (movieId: string) => {
  const { data } = await client.get(`/movies/${movieId}/screenings`);
  return data as ScreeningSummary[];
};

export const fetchSeatMap = async (screeningId: string) => {
  const { data } = await client.get(`/screenings/${screeningId}/seatmap`);
  return data as SeatMap;
};

export const postQuote = async (payload: QuoteRequestPayload) => {
  const { data } = await client.post('/quote', payload);
  return data as OrderQuote;
};

export const postBooking = async (payload: CheckoutRequest) => {
  const { data } = await client.post('/bookings', payload);
  return data as Booking;
};

export interface CastMember {
  name: string;
  character?: string | null;
  profileUrl?: string | null;
}

export interface Movie {
  id: string;
  title: string;
  synopsis: string;
  runtimeMinutes: number;
  rating: string;
  posterUrl: string;
  genres: string[];
  cast?: CastMember[];
}

export interface ScreeningSummary {
  screeningId: string;
  startUtc: string;
  cinemaId: string;
  cinemaName: string;
  auditoriumName: string;
  basePrice: number;
  class: string;
}

export interface SeatMap {
  screeningId: string;
  rows: number;
  columns: number;
  seats: Record<string, SeatState>;
}

export type SeatState = 'Available' | 'Selected' | 'Booked' | 'Blocked';

export interface SeatSelection {
  seatLabel: string;
  ticketTypeId: string;
}

export interface QuoteRequestPayload {
  screeningId: string;
  seats: SeatSelection[];
}

export interface OrderQuoteLine {
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface OrderQuote {
  screeningId: string;
  lines: OrderQuoteLine[];
  subtotal: number;
  discount: number;
  total: number;
}

export interface CheckoutRequest {
  screeningId: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  promoCode: string;
  seats: SeatSelection[];
  createAccount: boolean;
  password: string;
}

export interface BookingLine {
  seatLabel: string;
  ticketTypeId: string;
  unitPrice: number;
}

export interface Booking {
  id: string;
  screeningId: string;
  referenceCode: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  createdUtc: string;
  subtotal: number;
  discount: number;
  total: number;
  lines: BookingLine[];
}
