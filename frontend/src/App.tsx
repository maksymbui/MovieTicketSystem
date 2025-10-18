import { Routes, Route, Link, useLocation } from 'react-router-dom';
import { AppShell, Container, Group, Text, ActionIcon, Button } from '@mantine/core';
import { IconTicket } from '@tabler/icons-react';
import MoviesPage from './pages/MoviesPage';
import MovieDetailsPage from './pages/MovieDetailsPage';
import SeatSelectionPage from './pages/SeatSelectionPage';
import CheckoutPage from './pages/CheckoutPage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import MyOrdersPage from './pages/MyOrdersPage';
import AdminMenuPage from './pages/AdminMenuPage'
import DealsPage from './pages/DealsPage';
import { loadSession, clearSession } from '@/auth';
import ManageDealsPage from './pages/ManageDealsPage';


const App = () => {
  const location = useLocation();
  const isCatalogRoute = location.pathname === '/' || location.pathname === '/movies';
  const session = loadSession();

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
          <Group gap="sm">
            <Button component={Link} to="/deals" variant="light">Deals</Button>
              {!session && (
                <>
                  <Button component={Link} to="/login" variant="light">Sign in</Button>
                  <Button component={Link} to="/register" variant="outline">Register</Button>
                </>
              )}
              {session && (
                <>
                  <Button component={Link} to="/my-orders" variant="light">My Orders</Button>
                  {session.role === 'Admin' && (
                    <Button component={Link} to="/admin-menu" variant="light" color="yellow">Admin Panel</Button>
                  )}
                  <Button onClick={() => { clearSession(); window.location.reload(); }} variant="light" color="red">Logout</Button>
                </>
              )}
                  
            </Group>
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
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/my-orders" element={<MyOrdersPage />} />
            <Route path="/admin-menu" element={<AdminMenuPage />} />
            <Route path="/admin/manage-deals" element={<ManageDealsPage />} />
            <Route path="/deals" element={<DealsPage />} />
          </Routes>
        </Container>
      </AppShell.Main>
    </AppShell>
  );
};

export default App;
