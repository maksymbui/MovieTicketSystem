import { useEffect, useMemo, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Alert,
  AspectRatio,
  Badge,
  Button,
  Card,
  Divider,
  Grid,
  Group,
  MultiSelect,
  Paper,
  SegmentedControl,
  Skeleton,
  Stack,
  Text,
  Title
} from '@mantine/core';
import { IconAlertCircle, IconArmchair, IconBolt, IconCrown, IconMapPin, IconSparkles, IconTicket } from '@tabler/icons-react';
import { fetchMovies, fetchScreenings, Movie, ScreeningSummary } from '@/hooks/useApi';
import { sessionClassPalette } from '@/theme';

type CinemaOption = {
  value: string;
  label: string;
  state: string;
};

const STATE_LABELS: Record<string, string> = {
  ACT: 'ACT',
  NSW: 'NSW',
  NT: 'NT',
  QLD: 'QLD',
  SA: 'SA',
  TAS: 'TAS',
  VIC: 'VIC',
  WA: 'WA'
};

const DAY_RANGE = 7;

const MovieDetailsPage = () => {
  const { movieId } = useParams<{ movieId: string }>();
  const { data: movies = [], isLoading: moviesLoading } = useQuery({ queryKey: ['movies'], queryFn: fetchMovies });
  const movie = useMemo(() => movies.find((m) => m.id === movieId), [movies, movieId]);

  const { data: screenings = [], isLoading } = useQuery({
    queryKey: ['screenings', movieId],
    queryFn: () => fetchScreenings(movieId!),
    enabled: Boolean(movieId)
  });

  const cinemaOptions = useMemo<CinemaOption[]>(() => {
    const map = new Map<string, CinemaOption>();
    screenings.forEach((s) => {
      if (!map.has(s.cinemaId)) {
        map.set(s.cinemaId, {
          value: s.cinemaId,
          label: s.cinemaName,
          state: s.cinemaState ?? ''
        });
      }
    });
    return Array.from(map.values()).sort((a, b) => a.label.localeCompare(b.label));
  }, [screenings]);

  const stateSegments = useMemo(() => {
    const stateMap = new Map<string, string>();
    cinemaOptions.forEach(({ state }) => {
      if (!state) return;
      const label = STATE_LABELS[state] ?? state;
      stateMap.set(state, label);
    });
    return [
      { value: 'all', label: 'All states' },
      ...Array.from(stateMap.entries())
        .sort((a, b) => a[0].localeCompare(b[0]))
        .map(([value, label]) => ({ value, label }))
    ];
  }, [cinemaOptions]);

  const daySegments = useMemo(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const formatter = new Intl.DateTimeFormat(undefined, {
      weekday: 'short',
      month: 'short',
      day: 'numeric'
    });
    const segments = Array.from({ length: DAY_RANGE }, (_, offset) => {
      const date = new Date(today);
      date.setDate(date.getDate() + offset);
      const iso = date.toISOString().split('T')[0];
      if (offset === 0) return { value: iso, label: 'Today' };
      if (offset === 1) return { value: iso, label: 'Tomorrow' };
      return { value: iso, label: formatter.format(date) };
    });
    return [{ value: 'all', label: 'All days' }, ...segments];
  }, []);

  const defaultDayValue = useMemo(() => {
    const todaySegment = daySegments.find((segment) => segment.label === 'Today');
    if (todaySegment) return todaySegment.value;
    const firstNonAll = daySegments.find((segment) => segment.value !== 'all');
    return firstNonAll?.value ?? 'all';
  }, [daySegments]);

  const [selectedState, setSelectedState] = useState('all');
  const [selectedCinemas, setSelectedCinemas] = useState<string[]>([]);
  const [selectedDay, setSelectedDay] = useState(defaultDayValue);

  useEffect(() => {
    if (!daySegments.some((segment) => segment.value === selectedDay)) {
      setSelectedDay(defaultDayValue);
    }
  }, [daySegments, selectedDay, defaultDayValue]);

  useEffect(() => {
    if (!stateSegments.some((segment) => segment.value === selectedState)) {
      setSelectedState('all');
      return;
    }

    if (selectedState === 'all') return;
    const allowedIds = new Set(
      cinemaOptions.filter((option) => option.state === selectedState).map((opt) => opt.value)
    );
    setSelectedCinemas((prev) => prev.filter((id) => allowedIds.has(id)));
  }, [cinemaOptions, selectedState, stateSegments]);

  const filteredCinemaOptions = useMemo(() => {
    if (selectedState === 'all') return cinemaOptions;
    return cinemaOptions.filter((option) => option.state === selectedState);
  }, [cinemaOptions, selectedState]);

  const filteredScreenings = useMemo(() => {
    return screenings
      .filter((session) =>
        selectedState === 'all' ? true : session.cinemaState === selectedState
      )
      .filter((session) =>
        selectedCinemas.length === 0 ? true : selectedCinemas.includes(session.cinemaId)
      )
      .filter((session) =>
        selectedDay === 'all' || selectedDay === ''
          ? true
          : session.startUtc.startsWith(selectedDay)
      )
      .sort((a, b) => {
        const timeDiff = new Date(a.startUtc).getTime() - new Date(b.startUtc).getTime();
        if (timeDiff !== 0) return timeDiff;
        return stringCompare(a.cinemaName, b.cinemaName);
      });
  }, [screenings, selectedCinemas, selectedDay, selectedState]);

  const groupedByCinema = useMemo(() => {
    const map = new Map<
      string,
      { id: string; name: string; state: string; sessions: ScreeningSummary[] }
    >();
    filteredScreenings.forEach((session) => {
      if (!map.has(session.cinemaId)) {
        map.set(session.cinemaId, {
          id: session.cinemaId,
          name: session.cinemaName,
          state: session.cinemaState,
          sessions: []
        });
      }
      map.get(session.cinemaId)!.sessions.push(session);
    });
    const formatter = (value: ScreeningSummary) => new Date(value.startUtc).getTime();
    const classCompare = (a: ScreeningSummary, b: ScreeningSummary) =>
      stringCompare(a.class, b.class);
    const values = Array.from(map.values());
    values.forEach((entry) =>
      entry.sessions.sort((a, b) => {
        const timeDiff = formatter(a) - formatter(b);
        if (timeDiff !== 0) return timeDiff;
        return classCompare(a, b);
      })
    );
    return values.sort((a, b) => stringCompare(a.name, b.name));
  }, [filteredScreenings]);

  return (
    <Stack gap="xl">
      <MovieHero movie={movie} isLoading={moviesLoading} />

      <Paper bg="#1f1f1f" p="lg" radius="lg" withBorder>
        <Stack gap="xl">
          <Stack gap="xs">
            <Group gap="md" align="flex-start" wrap="nowrap">
              <IconMapPin size={18} />
              <div>
                <Title order={3} size="h3">
                  Sessions
                </Title>
                <Text size="sm" c="gray.4">
                  Filter cinemas and dates to find the session that suits you.
                </Text>
              </div>
            </Group>
          </Stack>

          <Grid gutter="lg" align="center">
            <Grid.Col span={{ base: 12, md: 4 }}>
              <Stack gap={4}>
                <Text size="sm" c="gray.4" fw={500}>
                  States
                </Text>
                <SegmentedControl
                  value={selectedState}
                  onChange={setSelectedState}
                  data={stateSegments}
                  fullWidth
                  color="tealAccent"
                  radius="md"
                />
              </Stack>
            </Grid.Col>
            <Grid.Col span={{ base: 12, md: 4 }}>
              <MultiSelect
                data={filteredCinemaOptions}
                value={selectedCinemas}
                onChange={setSelectedCinemas}
                placeholder="Filter by cinema"
                searchable
                clearable
                nothingFound="No cinemas in this state"
                label="Cinemas"
              />
            </Grid.Col>
            <Grid.Col span={{ base: 12, md: 4 }}>
              <Stack gap={4}>
                <Text size="sm" c="gray.4" fw={500}>
                  Date
                </Text>
                <SegmentedControl
                  value={selectedDay}
                  onChange={setSelectedDay}
                  data={daySegments}
                  fullWidth
                  color="tealAccent"
                  radius="md"
                  disabled={daySegments.length === 0}
                />
              </Stack>
            </Grid.Col>
          </Grid>

          {isLoading && (
            <SimpleSkeletonList />
          )}

          {!isLoading && groupedByCinema.length === 0 && (
            <Alert
              color="orange"
              variant="light"
              icon={<IconAlertCircle size={18} />}
            >
              There are no current sessions with the selected filters.
            </Alert>
          )}

          <Stack gap="lg">
            {groupedByCinema.map(({ id, name, state, sessions }) => (
              <Card
                key={id}
                withBorder
                padding="lg"
                radius="md"
                style={{ background: '#202020', borderColor: '#2f2f2f' }}
              >
                <Group justify="space-between" mb="md" align="flex-start">
                  <div>
                    <Title order={4}>{name}</Title>
                    <Text size="xs" c="gray.5">
                      {state ? `${state} • ` : ''}
                      {sessions.length} session{sessions.length > 1 ? 's' : ''} available
                    </Text>
                  </div>
                </Group>
                <Divider mb="md" color="rgba(255,255,255,0.05)" />
                <Group gap="sm">
                  {sessions.map((session) => {
                    const style = getSessionClassStyle(session.class);
                    const ClassIcon = getSessionClassIcon(session.class);
                    return (
                      <Button
                        key={session.screeningId}
                        component={Link}
                        to={`/screenings/${session.screeningId}/seats`}
                        radius="md"
                        style={{
                          backgroundColor: style.background,
                          borderColor: style.background,
                          color: style.foreground
                        }}
                      >
                        <Group gap="xs" align="center">
                          <ClassIcon size={16} color={style.foreground} />
                          <Text fw={600} size="sm" c={style.foreground}>
                            {formatSessionTime(session.startUtc)}
                          </Text>
                          <Badge
                            size="xs"
                            radius="sm"
                            variant="filled"
                            style={{
                              backgroundColor: style.foreground,
                              color: style.background
                            }}
                          >
                            {formatClassLabel(session.class)}
                          </Badge>
                        </Group>
                      </Button>
                    );
                  })}
                </Group>
              </Card>
            ))}
          </Stack>
        </Stack>
      </Paper>
    </Stack>
  );
};

const MovieHero = ({ movie, isLoading }: { movie?: Movie; isLoading: boolean }) => {
  if (isLoading) {
    return (
      <Card withBorder radius="lg" padding="lg" style={{ background: '#202020', borderColor: '#2f2f2f' }}>
        <Skeleton height={240} radius="lg" />
      </Card>
    );
  }

  if (!movie) {
    return (
      <Alert color="red" variant="light" icon={<IconAlertCircle size={18} />}>
        Movie not found.
      </Alert>
    );
  }

  const castLine = (() => {
    if (!movie.cast || movie.cast.length === 0) return null;
    const names = movie.cast
      .map((member) => member?.name?.trim())
      .filter((name): name is string => Boolean(name));
    if (names.length === 0) return null;
    return names.slice(0, 8).join(', ');
  })();

  return (
    <Card withBorder radius="lg" padding="lg" style={{ background: '#202020', borderColor: '#2f2f2f' }}>
      <Grid gutter="xl">
        <Grid.Col span={{ base: 12, md: 4 }}>
          <AspectRatio ratio={2 / 3}>
            {movie.posterUrl ? (
              <img
                src={movie.posterUrl}
                alt={movie.title}
                style={{
                  width: '100%',
                  height: '100%',
                  objectFit: 'cover',
                  borderRadius: '12px'
                }}
              />
            ) : (
              <PosterPlaceholder title={movie.title} />
            )}
          </AspectRatio>
        </Grid.Col>
        <Grid.Col span={{ base: 12, md: 8 }}>
          <Stack gap="md">
            <div>
              <Title order={2}>{movie.title}</Title>
              <Text size="sm" c="gray.4" mt={6}>
                {movie.synopsis}
              </Text>
              {castLine && (
                <Text size="sm" c="gray.3" mt="xs">
                  <Text component="span" fw={600} c="gray.1">
                    Cast:
                  </Text>{' '}
                  {castLine}
                </Text>
              )}
            </div>
            <Group gap="sm">
              <Badge color="charcoal" variant="light">
                {movie.rating}
              </Badge>
              <Badge color="charcoal" variant="light">
                {movie.runtimeMinutes} mins
              </Badge>
            </Group>
            <Group gap="xs">
              {movie.genres.map((genre) => (
                <Badge key={genre} color="copperAccent" variant="outline">
                  {genre}
                </Badge>
              ))}
            </Group>
            <Divider color="rgba(255,255,255,0.05)" />
            <Text size="sm" c="gray.4">
              Reserve seats instantly and collect loyalty rewards on checkout.
            </Text>
            <Button
              component={Link}
              to="/movies"
              variant="outline"
              color="tealAccent"
              leftSection={<IconTicket size={18} />}
              w={{ base: '100%', sm: 'auto' }}
            >
              Browse other titles
            </Button>
          </Stack>
        </Grid.Col>
      </Grid>
    </Card>
  );
};

const PosterPlaceholder = ({ title }: { title: string }) => (
  <Paper
    radius="lg"
    withBorder
    style={{
      background: 'linear-gradient(135deg, rgba(60,207,176,0.35), rgba(199,135,110,0.45))',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center'
    }}
  >
    <Stack gap={4} align="center" ta="center" px="md">
      <Text fw={600}>{title}</Text>
      <Text size="xs" c="gray.3">
        Poster coming soon
      </Text>
    </Stack>
  </Paper>
);

const SimpleSkeletonList = () => (
  <Stack gap="md">
    {Array.from({ length: 3 }).map((_, index) => (
      <Card
        key={index}
        withBorder
        radius="md"
        padding="lg"
        style={{ background: '#202020', borderColor: '#2f2f2f' }}
      >
        <Skeleton height={20} width="30%" radius="sm" />
        <Skeleton height={36} mt="sm" radius="sm" />
      </Card>
    ))}
  </Stack>
);

const CLASS_ICON_MAP = {
  Standard: IconArmchair,
  Deluxe: IconSparkles,
  VMax: IconBolt,
  GoldClass: IconCrown
} as const;

const getSessionClassIcon = (value: string) =>
  CLASS_ICON_MAP[value as keyof typeof CLASS_ICON_MAP] ?? IconArmchair;

const getSessionClassStyle = (value: string) =>
  sessionClassPalette[value as keyof typeof sessionClassPalette] ?? sessionClassPalette.Standard;

const formatClassLabel = (value: string) =>
  value.replace(/([a-z])([A-Z])/g, '$1 $2').trim();

const formatSessionTime = (iso: string) => {
  const date = new Date(iso);
  return date.toLocaleTimeString(undefined, {
    hour: 'numeric',
    minute: '2-digit'
  });
};

const stringCompare = (a: string, b: string) => a.localeCompare(b);

export default MovieDetailsPage;
