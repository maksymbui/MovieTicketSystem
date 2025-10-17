import { useState } from 'react';
import { TextInput, PasswordInput, Button, Group, Paper, Title, Stack, Alert } from '@mantine/core';
import { useNavigate, Link } from 'react-router-dom';
import { register, AuthResponse } from '@/hooks/useApi';
import { saveSession } from '@/auth';

const RegisterPage = () => {
  const nav = useNavigate();
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      const res: AuthResponse = await register(email, password, displayName || undefined);
      saveSession(res);
      nav('/');
    } catch (err) {
      setError('Sign up failed. Try a different email.');
    }
  };

  return (
    <Paper maw={420} mx="auto" mt="xl" p="lg" withBorder>
      <Title order={3} ta="center" mb="md">Create your account</Title>
      {error && <Alert color="red" mb="md">{error}</Alert>}
      <form onSubmit={onSubmit}>
        <Stack>
          <TextInput label="Email" value={email} onChange={(e) => setEmail(e.currentTarget.value)} required />
          <TextInput label="Display name" value={displayName} onChange={(e) => setDisplayName(e.currentTarget.value)} />
          <PasswordInput label="Password" value={password} onChange={(e) => setPassword(e.currentTarget.value)} required />
          <Group justify="space-between" mt="md">
            <Button type="submit">Sign up</Button>
            <Button variant="light" component={Link} to="/login">Back to sign in</Button>
          </Group>
        </Stack>
      </form>
    </Paper>
  );
};
export default RegisterPage;
