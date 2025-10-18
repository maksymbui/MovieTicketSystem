import { Alert, Button, Card, Stack, Title, Select, NumberInput, TextInput, Table, Text, ActionIcon} from '@mantine/core';
import { IconEdit, IconTrash } from '@tabler/icons-react';
import { loadSession } from '@/auth';
import { useNavigate } from 'react-router-dom';
import { useState, useEffect } from 'react';
import { fetchMovies, fetchDeals, createDeal, removeDeal, updateDeal } from '../hooks/useApi';

const ManageDealsPage = () => {
    const [error, setError] = useState('');
    const session = loadSession();
    const navigate = useNavigate();

    const [movies, setMovies] = useState([]);
    const [deals, setDeals] = useState([]);

    const movieOptions = movies.map(movie => ({
        value: movie.id,
        label: movie.title
    }));

    const [selectedMovieId, setSelectedMovieId] = useState('');
    const [discount, setDiscount] = useState<number | undefined>(undefined);
    const [expiryDate, setExpiryDate] = useState('');

    // For tracking which deal is being edited
    const [editingDealId, setEditingDealId] = useState('');
    const [editDiscount, setEditDiscount] = useState(0);
    const [editExpiryDate, setEditExpiryDate] = useState('');

    useEffect(() => {
    const loadData = async () => {
        try {
            const moviesData = await fetchMovies();
            const dealsData = await fetchDeals();
            setMovies(moviesData);
            setDeals(dealsData);
        } catch (error) {
            console.error('Failed to load data:', error);
        }
    };

    loadData();
    }, []);

    const handleCreateDeal = async () => {
        try {
            const newDeal = {
                movieId: selectedMovieId,
                discount: discount || 0,
                expiryDate: new Date(expiryDate)
            };
            
            const createdDeal = await createDeal(newDeal);
            
            // Reset form and refresh data
            setSelectedMovieId('');
            setDiscount(undefined);
            setExpiryDate('');
            
            // Refresh deals list
            const updatedDeals = await fetchDeals();
            setDeals(updatedDeals);
            
        } catch (error) {
            setError(error.response?.data?.message);
        }
    };

    const handleUpdateDeal = async (deal) => {
        try {
            const updatedDeal = {
                id: deal.id,
                movieId: deal.movieId,
                discount: editDiscount,
                expiryDate: new Date(editExpiryDate)
            };
            
            await updateDeal(updatedDeal);
            
            // Reset editing state
            setEditingDealId('');
            setEditDiscount(0);
            setEditExpiryDate('');
            
            // Refresh deals list
            const updatedDeals = await fetchDeals();
            setDeals(updatedDeals);
            
            setError('');
        } catch (error) {
            setError(error.response?.data?.message || 'Failed to update deal');
        }
    };

    const startEditing = (deal) => {
        setEditingDealId(deal.id);
        setEditDiscount(deal.discount);
        setEditExpiryDate(new Date(deal.expiryDate).toISOString().split('T')[0]);
    };

    const handleDeleteDeal = async (dealId) => {
        try {
            await removeDeal(dealId);
            
            // Refresh deals list
            const updatedDeals = await fetchDeals();
            setDeals(updatedDeals);
            
            setError(''); 
        } catch (error) {
            setError(error.response?.data?.message || 'Failed to delete deal');
        }
    };

    return (
        <Stack gap="xl">
        <Title order={2}>Deals Management </Title>
        {error && <Alert color="red" mb="md">{error}</Alert>}
        <Card withBorder padding="xl">
        <Stack gap="md">
            <Title order={3}>Add New Deal</Title>
            
            <Select
            label="Select Movie"
            placeholder="Choose a movie"
            data={movieOptions}
            value={selectedMovieId}
            onChange={(value) => setSelectedMovieId(value || '')}
            searchable
            />
            
            <NumberInput
            label="Discount (%)"
            placeholder="Enter discount percentage"
            value={discount}
            onChange={(value) => setDiscount(Number(value))}
            min={0}
            max={100}
            />
            
            <TextInput
            label="Expiry Date"
            type="date"
            value={expiryDate}
            onChange={(event) => setExpiryDate(event.currentTarget.value)}
            />
            
            <Button 
                onClick={handleCreateDeal}
                disabled={!selectedMovieId || discount === undefined || !expiryDate}
            >
                Create Deal
            </Button>
        </Stack>
        </Card>
        
        {/* All Deals Table */}
        <Card withBorder padding="xl">
            <Stack gap="md">
                <Title order={3}>All Deals</Title>
                
                {deals.length === 0 ? (
                    <Text c="dimmed">No deals found</Text>
                ) : (
                    <Table>
                        <Table.Thead>
                            <Table.Tr>
                                <Table.Th>Movie Name</Table.Th>
                                <Table.Th>Discount (%)</Table.Th>
                                <Table.Th>Expiry Date</Table.Th>
                                <Table.Th>Update</Table.Th>
                                <Table.Th>Delete</Table.Th>
                            </Table.Tr>
                        </Table.Thead>
                        <Table.Tbody>
                            {deals.map((deal) => {
                                const movie = movies.find(m => m.id === deal.movieId);
                                const isEditing = editingDealId === deal.id;
                                
                                return (
                                    <Table.Tr key={deal.id || deal.movieId}>
                                        <Table.Td>{movie?.title || 'Unknown Movie'}</Table.Td>
                                        <Table.Td>
                                            {isEditing ? (
                                                <NumberInput
                                                    value={editDiscount}
                                                    onChange={(value) => setEditDiscount(Number(value))}
                                                    min={0}
                                                    max={100}
                                                    size="sm"
                                                    style={{ width: 80 }}
                                                />
                                            ) : (
                                                `${deal.discount}%`
                                            )}
                                        </Table.Td>
                                        <Table.Td>
                                            {isEditing ? (
                                                <TextInput
                                                    type="date"
                                                    value={editExpiryDate}
                                                    onChange={(event) => setEditExpiryDate(event.currentTarget.value)}
                                                    size="sm"
                                                    style={{ width: 150 }}
                                                />
                                            ) : (
                                                new Date(deal.expiryDate).toLocaleDateString()
                                            )}
                                        </Table.Td>
                                        <Table.Td>
                                            {isEditing ? (
                                                <ActionIcon 
                                                    color="green"
                                                    onClick={() => handleUpdateDeal(deal)}
                                                >
                                                    <IconEdit size={16} />
                                                </ActionIcon>
                                            ) : (
                                                <ActionIcon 
                                                    color="blue"
                                                    onClick={() => startEditing(deal)}
                                                >
                                                    <IconEdit size={16} />
                                                </ActionIcon>
                                            )}
                                        </Table.Td>
                                        <Table.Td>
                                            <ActionIcon 
                                                color="red"
                                                onClick={() => handleDeleteDeal(deal.id)}
                                            >
                                                <IconTrash size={16} />
                                            </ActionIcon>
                                        </Table.Td>
                                    </Table.Tr>
                                );
                            })}
                        </Table.Tbody>
                    </Table>
                )}
            </Stack>
        </Card>
        
        </Stack>    );
};


export default ManageDealsPage;