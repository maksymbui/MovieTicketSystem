import { useEffect, useState } from 'react';
import { TextInput, Button, Group, Paper, Title, Stack, Alert, Table, Tabs } from '@mantine/core';
import { getMyBookings, getBookingByReference } from '@/hooks/useApi';
import { loadSession, clearSession } from '@/auth';
import { Link } from 'react-router-dom';

const MyOrdersPage = () => {
  const session = loadSession();
  const [reference, setReference] = useState('');
  const [error, setError] = useState('');
  const [one, setOne] = useState<any | null>(null);
  const [mine, setMine] = useState<any[]>([]);

  const fetchMine = async () => {
    setError('');
    try {
      const data = await getMyBookings();
      setMine(data);
    } catch {
      setError('Unable to load your bookings. Please sign in.');
    }
  };

  const searchOne = async () => {
    setError(''); setOne(null);
    if (!reference) { setError('Please enter a reference, e.g., BK123456'); return; }
    try {
      const data = await getBookingByReference(reference);
      setOne(data);
    } catch {
      setError('No booking found for that reference.');
    }
  };

  useEffect(() => { if (session) fetchMine(); }, []);

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>My Orders</Title>
        <Group>
          {!session && <Button component={Link} to="/login">Sign in</Button>}
          {!session && <Button variant="light" component={Link} to="/register">Register</Button>}
          {session && <Button variant="light" onClick={() => { clearSession(); location.reload(); }}>Sign out</Button>}
        </Group>
      </Group>

      {error && <Alert color="red">{error}</Alert>}

      <Tabs defaultValue="mine">
        <Tabs.List>
          <Tabs.Tab value="mine">My bookings</Tabs.Tab>
          <Tabs.Tab value="ref">Find by reference</Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="mine" pt="md">
          {!session ? (
            <Alert color="yellow">Sign in to view your bookings.</Alert>
          ) : (
            <Table>
              <Table.Thead>
                <Table.Tr><Table.Th>Reference</Table.Th><Table.Th>Name</Table.Th><Table.Th>Email</Table.Th><Table.Th>Total</Table.Th></Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {mine.map((b) => (
                  <Table.Tr key={b.referenceCode}>
                    <Table.Td>{b.referenceCode}</Table.Td>
                    <Table.Td>{b.customerName}</Table.Td>
                    <Table.Td>{b.customerEmail}</Table.Td>
                    <Table.Td>${b.total?.toFixed?.(2) ?? b.total}</Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          )}
        </Tabs.Panel>

        <Tabs.Panel value="ref" pt="md">
          <Paper p="md" withBorder radius="md">
            <Group align="end">
              <TextInput label="Booking reference" placeholder="BK123456" value={reference} onChange={(e)=>setReference(e.currentTarget.value)} />
              <Button onClick={searchOne}>Search</Button>
            </Group>
            {one && (
              <Table mt="md">
                <Table.Tbody>
                  <Table.Tr><Table.Td>Reference</Table.Td><Table.Td>{one.referenceCode}</Table.Td></Table.Tr>
                  <Table.Tr><Table.Td>Name</Table.Td><Table.Td>{one.customerName}</Table.Td></Table.Tr>
                  <Table.Tr><Table.Td>Email</Table.Td><Table.Td>{one.customerEmail}</Table.Td></Table.Tr>
                  <Table.Tr><Table.Td>Total</Table.Td><Table.Td>${one.total?.toFixed?.(2) ?? one.total}</Table.Td></Table.Tr>
                </Table.Tbody>
              </Table>
            )}
          </Paper>
        </Tabs.Panel>
      </Tabs>
    </Stack>
  );
};
export default MyOrdersPage;