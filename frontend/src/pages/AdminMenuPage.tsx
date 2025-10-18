import { Alert, Button, Card, Stack, Title } from '@mantine/core';
import { loadSession } from '@/auth';
import { useNavigate } from 'react-router-dom';

const AdminMenuPage = () => {
  const session = loadSession();
  const navigate = useNavigate();

  if (session?.role !== 'Admin') {
    return (
      <Alert color="red">
        Access denied. Admin privileges required.
      </Alert>
    );
  }

  return (
    <Stack gap="xl">
      <Title order={2}>Admin Panel</Title>
      
      <Card withBorder padding="xl" radius="lg" style={{ background: '#202020', borderColor: '#2f2f2f' }}>
        <Stack gap="md">
          <Title order={3}>Welcome, Administrator</Title>
          
          <Button 
            variant="filled" 
            size="lg"
            onClick={() => navigate('/admin/manage-deals')}
          >
            Manage Deals
          </Button>
          
        </Stack>
      </Card>
    </Stack>
  );
};

export default AdminMenuPage;