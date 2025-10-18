import { Alert, Button, Card, Stack, Title, Text, Group, AspectRatio, Skeleton } from '@mantine/core';
import { loadSession } from '@/auth';
import { useNavigate } from 'react-router-dom';
import { useState, useEffect } from 'react';
import { fetchDeals, fetchMovies } from '../hooks/useApi';
import type { Deal, Movie } from '../hooks/useApi';

const DealsPage = () => {
  const session = loadSession();
  const navigate = useNavigate();

  const [deals, setDeals] = useState<Deal[]>([]);
  const [movies, setMovies] = useState<Movie[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadData = async () => {
      try {
        setIsLoading(true);
        const [dealsData, moviesData] = await Promise.all([
          fetchDeals(),
          fetchMovies()
        ]);
        setDeals(dealsData);
        setMovies(moviesData);
      } catch (error) {
        console.error('Failed to load data:', error);
      } finally {
        setIsLoading(false);
      }
    };

    loadData();
  }, []);

  // Filter active deals (not expired)
  const activeDeals = deals.filter(deal => new Date(deal.expiryDate) > new Date());

  if (isLoading) {
    return (
      <Stack gap="xl">
        <Title order={2}>Deals</Title>
        <Stack gap="md">
          {Array.from({ length: 3 }).map((_, index) => (
            <Card key={index} withBorder padding="xl" radius="lg" style={{ background: '#202020', borderColor: '#2f2f2f' }}>
              <Group gap="xl" align="stretch">
                <Skeleton height={200} width={133} radius="lg" />
                <Stack gap="sm" style={{ flex: 1 }}>
                  <Skeleton height={24} width="70%" />
                  <Skeleton height={16} width="50%" />
                  <Skeleton height={32} width="120px" />
                </Stack>
              </Group>
            </Card>
          ))}
        </Stack>
      </Stack>
    );
  }

  if (activeDeals.length === 0) {
    return (
      <Stack gap="xl">
        <Title order={2}>Deals</Title>
        <Alert color="orange" variant="light">
          No active deals available at the moment. Check back soon for exciting offers!
        </Alert>
      </Stack>
    );
  }

  return (
    <Stack gap="xl">
      <Title order={2}>Deals</Title>
      
      <Stack gap="md">
        {activeDeals.map((deal) => {
          const movie = movies.find(m => m.id === deal.movieId);
          return (
            <DealCard key={deal.movieId} deal={deal} movie={movie} />
          );
        })}
      </Stack>
    </Stack>
  );
};

interface DealCardProps {
  deal: Deal;
  movie?: Movie;
}

const DealCard = ({ deal, movie }: DealCardProps) => {
  const navigate = useNavigate();
  
  const formatExpiryDate = (expiryDate: Date) => {
    return new Date(expiryDate).toLocaleDateString(undefined, {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  };

  const handleGetTickets = () => {
    if (movie) {
      navigate(`/movies/${movie.id}`);
    }
  };

  return (
    <Card withBorder padding="xl" radius="lg" style={{ background: '#202020', borderColor: '#2f2f2f' }}>
      <Group gap="xl" align="stretch">
        {/* Movie Poster */}
        <div style={{ width: 133, flexShrink: 0 }}>
          <AspectRatio ratio={2 / 3}>
            {movie?.posterUrl ? (
              <img
                src={movie.posterUrl}
                alt={movie.title}
                style={{
                  width: '100%',
                  height: '100%',
                  objectFit: 'cover',
                  borderRadius: '8px'
                }}
              />
            ) : (
              <div
                style={{
                  width: '100%',
                  height: '100%',
                  background: 'linear-gradient(135deg, rgba(60,207,176,0.35), rgba(199,135,110,0.45))',
                  borderRadius: '8px',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  textAlign: 'center',
                  padding: '8px'
                }}
              >
                <Text size="sm" fw={600}>
                  {movie?.title || 'Movie'}
                </Text>
              </div>
            )}
          </AspectRatio>
        </div>

        <Stack gap="sm" style={{ flex: 1 }}>
          <Text size="xl" fw={700} c="teal.4">
            Get {movie?.title} with {deal.discount}% off!
          </Text>
          
          <Text size="md" c="gray.3">
            Don't miss out on this amazing deal - save {deal.discount}% on all tickets for {movie?.title}.
          </Text>
          
          <Text size="sm" c="orange.4" fw={500}>
            Offer expires: {formatExpiryDate(deal.expiryDate)}
          </Text>

          <Group mt="md">
            <Button
              color="teal"
              size="md"
              onClick={handleGetTickets}
              disabled={!movie}
            >
              Get Tickets Now
            </Button>
          </Group>
        </Stack>
      </Group>
    </Card>
  );
};

export default DealsPage;