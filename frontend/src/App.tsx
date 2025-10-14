import { Routes, Route, Link, useLocation } from 'react-router-dom';
import { AppShell, Container, Group, Text, ActionIcon, Button } from '@mantine/core';
import { IconTicket } from '@tabler/icons-react';
import MoviesPage from './pages/MoviesPage';
import MovieDetailsPage from './pages/MovieDetailsPage';
import SeatSelectionPage from './pages/SeatSelectionPage';
import CheckoutPage from './pages/CheckoutPage';

const App = () => {
  const location = useLocation();
  const isCatalogRoute = location.pathname === '/' || location.pathname === '/movies';

  return (
    <AppShell
      padding="xl"
      styles={{
        main: {
          background: 'radial-gradient(circle at top, rgba(76, 255, 198, 0.08), transparent 55%), #181818',
          color: '#faf9f6'
        }
      }}
      header={{ height: 72 }}
    >
      <AppShell.Header
        style={{
          backgroundColor: '#181818',
          borderBottom: '1px solid rgba(255, 255, 255, 0.05)'
        }}
      >
        <Container size="xl" style={{ height: '100%' }}>
          <Group justify="space-between" align="center" h="100%">
            <Group gap="sm">
              <ActionIcon size="lg" variant="light" color="tealAccent" radius="md">
                <IconTicket size={20} />
              </ActionIcon>
              <div>
                <Text fw={600} size="lg">
                  Movie Tickets
                </Text>
                <Text size="xs" c="gray.4">
                  Discover, select, enjoy the perfect session.
                </Text>
              </div>
            </Group>

            {!isCatalogRoute && (
              <Button component={Link} to="/movies" variant="subtle" color="tealAccent">
                Back to catalog
              </Button>
            )}
          </Group>
        </Container>
      </AppShell.Header>

      <AppShell.Main>
        <Container size="xl">
          <Routes>
            <Route path="/" element={<MoviesPage />} />
            <Route path="/movies" element={<MoviesPage />} />
            <Route path="/movies/:movieId" element={<MovieDetailsPage />} />
            <Route path="/screenings/:screeningId/seats" element={<SeatSelectionPage />} />
            <Route path="/screenings/:screeningId/checkout" element={<CheckoutPage />} />
          </Routes>
        </Container>
      </AppShell.Main>
    </AppShell>
  );
};

export default App;
