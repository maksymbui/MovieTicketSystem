import axios from 'axios';

const baseURL = import.meta.env.DEV ? '/api' : 'http://localhost:5000/api';

const client = axios.create({
  baseURL
});

// attach token if present
client.interceptors.request.use((config) => {
  const raw = localStorage.getItem('mts_session');
  if (raw) {
    try { const s = JSON.parse(raw); if (s.token) config.headers['Authorization'] = `Bearer ${s.token}`; } catch {}
  }
  return config;
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

export const fetchTicketTypes = async () => {
  const { data } = await client.get('/ticket-types');
  return data as TicketType[];
};

export const postQuote = async (payload: QuoteRequestPayload) => {
  const { data } = await client.post('/quote', payload);
  return data as OrderQuote;
};

export const postBooking = async (payload: CheckoutRequest) => {
  const { data } = await client.post('/bookings', payload);
  return data as Booking;
};

export const fetchDeals = async () => {
  const { data } = await client.get('/deals');
  return data as Deal[];
}

export const updateDeal = async (payload: Deal) => {
  const { data } = await client.post(`/deals/update`, payload);
  return data as Deal;
}

export const removeDeal = async (payload: string) => {
  const { data } = await client.post(`/deals/remove`, { id: payload });
  return data as { success: boolean };
}

export const createDeal = async (payload: Deal) => {
  const { data } = await client.post('/deals/add', payload);
  return data as Deal;
}

export const fetchMessages = async (userId: string) => {
  const { data } = await client.get(`/messages/${userId}`);
  return data as Message[];
}

export interface CastMember {
  name: string;
  character?: string | null;
  profileUrl?: string | null;
}

export interface Deal {
  movieId: string;
  discount: number;
  expiryDate: Date;
}

export interface Message {
  id: string;
  toUserId: string;
  content: string;
  sentUtc: string;
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
  cinemaState: string;
  auditoriumName: string;
  basePrice: number;
  class: string;
  movie?: Movie;
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

export interface TicketType {
  id: string;
  category: string;
  name: string;
  price: number;
  requiresMembership: boolean;
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
  seatLabel: string;
  ticketTypeId: string;
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


export type AuthResponse = { token: string; userId: string; email: string; displayName: string; role: string };
export const login = async (email: string, password: string) => {
  const { data } = await client.post('/auth/login', { email, password });
  return data as AuthResponse;
};
export const register = async (email: string, password: string, displayName?: string) => {
  const { data } = await client.post('/auth/register', { email, password, displayName });
  return data as AuthResponse;
};
export const getMyBookings = async () => {
  const { data } = await client.get('/my/bookings');
  return data as Booking[];
};
export const getBookingByReference = async (reference: string) => {
  const { data } = await client.get(`/bookings/${reference}`);
  return data as Booking;
};
