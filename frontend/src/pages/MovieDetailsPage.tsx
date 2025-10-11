import { useMemo, useState } from 'react';
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
import { IconAlertCircle, IconClock, IconMapPin, IconTicket } from '@tabler/icons-react';
import { fetchMovies, fetchScreenings, Movie, ScreeningSummary } from '@/hooks/useApi';

const MovieDetailsPage = () => {
  const { movieId } = useParams<{ movieId: string }>();
  const { data: movies = [], isLoading: moviesLoading } = useQuery({ queryKey: ['movies'], queryFn: fetchMovies });
  const movie = useMemo(() => movies.find((m) => m.id === movieId), [movies, movieId]);

  const { data: screenings = [], isLoading } = useQuery({
    queryKey: ['screenings', movieId],
    queryFn: () => fetchScreenings(movieId!),
    enabled: Boolean(movieId)
  });

  const cinemaOptions = useMemo(() => {
    const unique = Array.from(
      new Map(screenings.map((s) => [s.cinemaId, s.cinemaName])).entries()
    ).map(([value, label]) => ({ value, label }));
    return unique.sort((a, b) => a.label.localeCompare(b.label));
  }, [screenings]);

  const daySegments = useMemo(() => {
    const unique = new Set(
      screenings.map((s) => new Date(s.startUtc).toISOString().split('T')[0])
    );
    const sorted = Array.from(unique).sort();
    const formatter = new Intl.DateTimeFormat(undefined, {
      weekday: 'short',
      month: 'short',
      day: 'numeric'
    });
    const todayIso = new Date().toISOString().split('T')[0];
    const tomorrowIso = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString().split('T')[0];

    return [
      { value: 'all', label: 'All days' },
      ...sorted.map((iso) => {
        if (iso === todayIso) return { value: iso, label: 'Today' };
        if (iso === tomorrowIso) return { value: iso, label: 'Tomorrow' };
        return { value: iso, label: formatter.format(new Date(iso)) };
      })
    ];
  }, [screenings]);

  const [selectedCinemas, setSelectedCinemas] = useState<string[]>([]);
  const [selectedDay, setSelectedDay] = useState(daySegments[0]?.value ?? 'all');

  const filteredScreenings = useMemo(() => {
    return screenings
      .filter((session) =>
        selectedCinemas.length === 0 ? true : selectedCinemas.includes(session.cinemaId)
      )
      .filter((session) =>
        selectedDay === 'all' ? true : session.startUtc.startsWith(selectedDay)
      )
      .sort((a, b) => {
        const timeDiff = new Date(a.startUtc).getTime() - new Date(b.startUtc).getTime();
        if (timeDiff !== 0) return timeDiff;
        return stringCompare(a.cinemaName, b.cinemaName);
      });
  }, [screenings, selectedCinemas, selectedDay]);

  const groupedByCinema = useMemo(() => {
    const map = new Map<string, ScreeningSummary[]>();
    filteredScreenings.forEach((session) => {
      if (!map.has(session.cinemaName)) map.set(session.cinemaName, []);
      map.get(session.cinemaName)!.push(session);
    });
    return Array.from(map.entries());
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
            <Grid.Col span={{ base: 12, md: 6 }}>
              <MultiSelect
                data={cinemaOptions}
                value={selectedCinemas}
                onChange={setSelectedCinemas}
                placeholder="Filter by cinema"
                searchable
                clearable
                label="Cinemas"
              />
            </Grid.Col>
            <Grid.Col span={{ base: 12, md: 6 }}>
              <SegmentedControl
                value={selectedDay}
                onChange={setSelectedDay}
                data={daySegments}
                fullWidth
              />
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
              No sessions match the selected filters.
            </Alert>
          )}

          <Stack gap="lg">
            {groupedByCinema.map(([cinemaName, cinemaSessions]) => (
              <Card
                key={cinemaName}
                withBorder
                padding="lg"
                radius="md"
                style={{ background: '#202020', borderColor: '#2f2f2f' }}
              >
                <Group justify="space-between" mb="md" align="flex-start">
                  <div>
                    <Title order={4}>{cinemaName}</Title>
                    <Text size="xs" c="gray.5">
                      {cinemaSessions.length} session{cinemaSessions.length > 1 ? 's' : ''} available
                    </Text>
                  </div>
                </Group>
                <Divider mb="md" color="rgba(255,255,255,0.05)" />
                <Group gap="sm">
                  {cinemaSessions.map((session) => (
                    <Button
                      key={session.screeningId}
                      component={Link}
                      to={`/screenings/${session.screeningId}/seats`}
                      variant="light"
                      color="tealAccent"
                      radius="md"
                      leftSection={<IconClock size={16} />}
                    >
                      {formatSessionTime(session.startUtc)}
                      <Text size="xs" c="gray.6" ml={8}>
                        {session.class}
                      </Text>
                    </Button>
                  ))}
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

const formatSessionTime = (iso: string) => {
  const date = new Date(iso);
  return date.toLocaleString(undefined, {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit'
  });
};

const stringCompare = (a: string, b: string) => a.localeCompare(b);

export default MovieDetailsPage;
