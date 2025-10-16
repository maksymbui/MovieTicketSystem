import { useState } from 'react';
import { TextInput, PasswordInput, Button, Group, Paper, Title, Stack, Alert } from '@mantine/core';
import { useNavigate, Link } from 'react-router-dom';
import { login, AuthResponse } from '@/hooks/useApi';
import { saveSession } from '@/auth';

const LoginPage = () => {
  const nav = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      const res: AuthResponse = await login(email, password);
      saveSession(res);
      nav('/my-orders');
    } catch (err: any) {
      setError('Login failed. Check email/password.');
    }
  };

  return (
    <Paper p="xl" radius="lg" withBorder>
      <Title order={2} mb="md">Sign in</Title>
      {error && <Alert color="red" mb="md">{error}</Alert>}
      <form onSubmit={onSubmit}>
        <Stack>
          <TextInput label="Email" value={email} onChange={(e) => setEmail(e.currentTarget.value)} required />
          <PasswordInput label="Password" value={password} onChange={(e) => setPassword(e.currentTarget.value)} required />
          <Group justify="space-between" mt="md">
            <Button type="submit">Sign in</Button>
            <Button variant="light" component={Link} to="/register">Create account</Button>
          </Group>
        </Stack>
      </form>
    </Paper>
  );
};
export default LoginPage;