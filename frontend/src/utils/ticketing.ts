import { OrderQuote, SeatSelection, TicketType } from '@/hooks/useApi';

export type TicketCounts = Record<string, number>;

export interface SummaryLine {
  ticketType: TicketType;
  count: number;
  total: number;
}

export const compareSeatLabels = (a: string, b: string) => {
  const rowA = a.charCodeAt(0);
  const rowB = b.charCodeAt(0);
  if (rowA !== rowB) return rowA - rowB;

  const colA = parseInt(a.slice(1), 10);
  const colB = parseInt(b.slice(1), 10);
  return colA - colB;
};

export const clampTicketCountsToTarget = (
  counts: TicketCounts,
  ticketTypes: TicketType[],
  target: number
): TicketCounts => {
  const total = Object.values(counts).reduce((sum, value) => sum + value, 0);
  if (total <= target) return counts;

  const orderedIds: string[] = ticketTypes.map((type) => type.id);
  Object.keys(counts).forEach((id) => {
    if (!orderedIds.includes(id)) orderedIds.push(id);
  });

  const next: TicketCounts = { ...counts };
  let remaining = total;

  for (let i = orderedIds.length - 1; i >= 0 && remaining > target; i -= 1) {
    const id = orderedIds[i];
    let value = next[id] ?? 0;
    if (value <= 0) continue;

    while (value > 0 && remaining > target) {
      value -= 1;
      remaining -= 1;
    }

    if (value <= 0) delete next[id];
    else next[id] = value;
  }

  return next;
};

export const buildSeatAssignments = (
  seatLabels: string[],
  counts: TicketCounts,
  ticketTypes: TicketType[]
): SeatSelection[] => {
  if (seatLabels.length === 0) return [];

  const sortedSeats = [...seatLabels].sort(compareSeatLabels);
  const assignments: SeatSelection[] = [];
  let cursor = 0;

  const appendAssignments = (typeId: string, count: number) => {
    for (let i = 0; i < count && cursor < sortedSeats.length; i += 1) {
      assignments.push({
        seatLabel: sortedSeats[cursor],
        ticketTypeId: typeId
      });
      cursor += 1;
    }
  };

  ticketTypes.forEach((type) => {
    const count = counts[type.id] ?? 0;
    appendAssignments(type.id, count);
  });

  Object.entries(counts)
    .filter(([id]) => !ticketTypes.some((type) => type.id === id))
    .forEach(([id, count]) => appendAssignments(id, count));

  return assignments;
};

export const summariseQuote = (
  quote: OrderQuote | undefined,
  seatAssignments: SeatSelection[],
  ticketTypes: TicketType[]
): SummaryLine[] => {
  if (!seatAssignments.length) return [];

  const typeMap = new Map(ticketTypes.map((type) => [type.id, type]));
  const seatTypeMap = new Map(seatAssignments.map((assignment) => [assignment.seatLabel, assignment.ticketTypeId]));
  const summaryMap = new Map<string, SummaryLine>();

  if (quote) {
    quote.lines.forEach((line) => {
      const typeId = line.ticketTypeId || seatTypeMap.get(line.seatLabel);
      if (!typeId) return;

      const ticketType = typeMap.get(typeId);
      if (!ticketType) return;

      const existing = summaryMap.get(typeId) ?? { ticketType, count: 0, total: 0 };
      existing.count += line.quantity;
      existing.total += line.lineTotal;
      summaryMap.set(typeId, existing);
    });
  }

  if (summaryMap.size === 0) {
    seatAssignments.forEach((assignment) => {
      const ticketType = typeMap.get(assignment.ticketTypeId);
      if (!ticketType) return;

      const existing = summaryMap.get(assignment.ticketTypeId) ?? { ticketType, count: 0, total: 0 };
      existing.count += 1;
      existing.total += ticketType.price;
      summaryMap.set(assignment.ticketTypeId, existing);
    });
  }

  const order = ticketTypes.map((type) => type.id);
  return Array.from(summaryMap.values()).sort((a, b) => order.indexOf(a.ticketType.id) - order.indexOf(b.ticketType.id));
};

export const totalFromSummary = (summary: SummaryLine[]) =>
  summary.reduce((sum, line) => sum + line.total, 0);
