import { Alert, Button, Card, Stack, Title, Text, Box, Group } from '@mantine/core';
import { loadSession } from '../auth';
import { useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { fetchMessages, Message } from '../hooks/useApi';

const MailBoxPage = () => {
  const session = loadSession();
  const navigate = useNavigate();
  const [messages, setMessages] = useState<Message[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadMessages = async () => {
      try {
        setLoading(true);
        const data = await fetchMessages(session!.userId);
        setMessages(data);
      } catch (err) {
        setError('Failed to load messages');
        console.error('Error fetching messages:', err);
      } finally {
        setLoading(false);
      }
    };

    loadMessages();
  }, []);

  if (session?.role !== 'Admin') {
    return (
      <Alert color="red">
        Access denied. Admin privileges required.
      </Alert>
    );
  }

  const formatDateTime = (utcDateString: string) => {
    const date = new Date(utcDateString);
    return date.toLocaleString();
  };

  return (
    <Stack gap="xl">
      <Group justify="space-between">
        <Title order={2}>MailBox</Title>
      </Group>
      
      {loading && <Text>Loading messages...</Text>}
      {error && <Alert color="red">{error}</Alert>}
      
      {!loading && !error && (
        <Stack gap="md">
          {messages.length === 0 ? (
            <Card withBorder padding="xl" radius="lg" style={{ background: '#202020', borderColor: '#2f2f2f' }}>
              <Text c="dimmed" ta="center">No messages found</Text>
            </Card>
          ) : (
            messages.map((message) => (
              <Card 
                key={message.id} 
                withBorder 
                padding="lg" 
                radius="lg" 
                style={{ background: '#202020', borderColor: '#2f2f2f' }}
              >
                <Stack gap="sm">
                  <Group justify="space-between">
                    <Text fw={500}>To User: {message.toUserId}</Text>
                    <Text size="sm" c="dimmed">
                      {formatDateTime(message.sentUtc)}
                    </Text>
                  </Group>
                  <Box>
                    <Text>{message.content}</Text>
                  </Box>
                </Stack>
              </Card>
            ))
          )}
        </Stack>
      )}
    </Stack>
  );
};

export default MailBoxPage;