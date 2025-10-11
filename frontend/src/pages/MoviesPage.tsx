import { useMemo, useState, useEffect, useCallback, useRef } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  ActionIcon,
  Button,
  Card,
  Group,
  Skeleton,
  Stack,
  Text,
  TextInput,
  Tooltip
} from '@mantine/core';
import { IconSearch, IconTicket, IconX } from '@tabler/icons-react';
import { fetchMovies, Movie } from '@/hooks/useApi';
import styles from './MoviesPage.module.css';

const CARD_WIDTH = 240;
const GRID_GAP = 24;

const MoviesPage = () => {
  const { data: movies = [], isLoading } = useQuery({
    queryKey: ['movies'],
    queryFn: fetchMovies
  });
  const [search, setSearch] = useState('');
  const gridRef = useRef<HTMLDivElement | null>(null);
  const [layout, setLayout] = useState({ columns: 1, width: 0 });

  const recalcColumns = useCallback(() => {
    const element = gridRef.current;
    if (!element) return;
    const width = element.clientWidth;
    const columns = Math.max(1, Math.floor((width + GRID_GAP) / (CARD_WIDTH + GRID_GAP)));
    setLayout({ columns, width });
  }, []);

  useEffect(() => {
    recalcColumns();
    window.addEventListener('resize', recalcColumns);
    return () => window.removeEventListener('resize', recalcColumns);
  }, [recalcColumns]);

  useEffect(() => {
    recalcColumns();
  }, [movies.length, recalcColumns]);

  const filtered = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) return movies;
    return movies.filter((m) => m.title.toLowerCase().includes(query));
  }, [movies, search]);

  const placeholders = useMemo(() => {
    if (isLoading || filtered.length === 0) return 0;
    const remainder = filtered.length % layout.columns;
    return remainder === 0 ? 0 : layout.columns - remainder;
  }, [layout.columns, filtered.length, isLoading]);

  const columnGapValue = layout.columns > 1
    ? Math.max(GRID_GAP, (layout.width - layout.columns * CARD_WIDTH) / (layout.columns - 1))
    : 0;

  const itemsToRender = isLoading
    ? Array.from({ length: layout.columns }, () => undefined)
    : filtered;

  return (
    <Stack gap="xl">
      <Stack gap="xs">
        <Text size="xl" fw={600}>
          Now showing
        </Text>
        <Text size="sm" c="gray.4">
          Browse the latest releases and lock in your seats in seconds.
        </Text>
      </Stack>

      <Group gap="md" wrap="wrap">
        <TextInput
          value={search}
          onChange={(event) => setSearch(event.currentTarget.value)}
          placeholder="Search by title"
          leftSection={<IconSearch size={16} />}
          rightSection={
            search ? (
              <Tooltip label="Clear search">
                <ActionIcon variant="subtle" color="gray" onClick={() => setSearch('')} aria-label="Clear search">
                  <IconX size={14} />
                </ActionIcon>
              </Tooltip>
            ) : null
          }
          size="md"
          w={{ base: '100%', sm: '24rem' }}
        />
      </Group>

      <div
        ref={gridRef}
        className={styles.catalogGrid}
        style={{
          gridTemplateColumns: `repeat(${layout.columns}, ${CARD_WIDTH}px)`,
          columnGap: layout.columns > 1 ? `${columnGapValue}px` : 0,
          rowGap: `${GRID_GAP}px`
        }}
      >
        {itemsToRender.map((movie, index) =>
          movie ? (
            <MovieCard key={movie.id} movie={movie} />
          ) : (
            <LoadingCard key={`skeleton-${index}`} />
          )
        )}

        {!isLoading && placeholders > 0 &&
          Array.from({ length: placeholders }).map((_, index) => (
            <div key={`placeholder-${index}`} className={`${styles.catalogCard} ${styles.placeholderCard}`} aria-hidden="true" />
          ))}
      </div>
    </Stack>
  );
};

const MovieCard = ({ movie }: { movie: Movie }) => (
  <Card withBorder radius="lg" className={styles.catalogCard} padding="lg">
    <div className={styles.catalogPoster}>
      {movie.posterUrl ? (
        <img src={movie.posterUrl} alt={movie.title} />
      ) : (
        <div className={styles.catalogFallback}>{movie.title}</div>
      )}
    </div>

    <Stack gap="sm" mt="md" style={{ flexGrow: 1 }}>
      <Text fw={600} size="sm" lineClamp={2} ta="center" className={styles.catalogTitle}>
        {movie.title}
      </Text>

      <Button
        component={Link}
        to={`/movies/${movie.id}`}
        color="tealAccent"
        rightSection={<IconTicket size={18} stroke={1.5} />}
        fullWidth
        style={{ marginTop: 'auto' }}
        className={styles.catalogButton}
      >
        Get tickets
      </Button>
    </Stack>
  </Card>
);

const LoadingCard = () => (
  <Card withBorder radius="lg" className={styles.catalogCard} padding="lg">
    <Skeleton height={300} radius="md" />
    <Skeleton height={20} mt="md" />
    <Skeleton height={36} mt="sm" radius="md" />
  </Card>
);

export default MoviesPage;
