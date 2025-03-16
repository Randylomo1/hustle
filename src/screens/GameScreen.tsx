import React, { useState, useEffect } from 'react';
import { View, StyleSheet, Text, TouchableOpacity, FlatList } from 'react-native';
import MapView, { Marker } from 'react-native-maps';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Delivery, DeliveryStatus, Player, Vehicle } from '../types';

interface GameScreenProps {
    player: Player;
    onDeliveryAccept: (delivery: Delivery) => void;
    onWithdraw: () => void;
}

export const GameScreen: React.FC<GameScreenProps> = ({
    player,
    onDeliveryAccept,
    onWithdraw,
}) => {
    const [selectedDelivery, setSelectedDelivery] = useState<Delivery | null>(null);
    const [availableDeliveries, setAvailableDeliveries] = useState<Delivery[]>([]);
    const [currentRegion, setCurrentRegion] = useState({
        latitude: -1.2921,  // Nairobi coordinates
        longitude: 36.8219,
        latitudeDelta: 0.0922,
        longitudeDelta: 0.0421,
    });

    useEffect(() => {
        // In production, this would fetch real deliveries from a backend
        const mockDeliveries: Delivery[] = [
            {
                id: '1',
                pickupLocation: {
                    latitude: -1.2850,
                    longitude: 36.8200,
                    address: 'CBD, Nairobi'
                },
                dropoffLocation: {
                    latitude: -1.3000,
                    longitude: 36.8300,
                    address: 'South B, Nairobi'
                },
                distance: 5.2,
                reward: 250,
                timeLimit: 30,
                status: DeliveryStatus.AVAILABLE,
                difficulty: 1,
                requiredVehicleType: player.vehicles[0]?.type
            },
            // Add more mock deliveries here
        ];

        setAvailableDeliveries(mockDeliveries);
    }, []);

    const renderDeliveryItem = ({ item }: { item: Delivery }) => (
        <TouchableOpacity
            style={styles.deliveryItem}
            onPress={() => setSelectedDelivery(item)}
        >
            <Text style={styles.deliveryTitle}>Delivery to {item.dropoffLocation.address}</Text>
            <Text>Distance: {item.distance.toFixed(1)} km</Text>
            <Text style={styles.rewardText}>Reward: KES {item.reward}</Text>
            <Text>Time Limit: {item.timeLimit} minutes</Text>
        </TouchableOpacity>
    );

    return (
        <SafeAreaView style={styles.container}>
            <View style={styles.header}>
                <View style={styles.playerInfo}>
                    <Text style={styles.playerName}>{player.username}</Text>
                    <Text style={styles.balance}>KES {player.balance}</Text>
                </View>
                <TouchableOpacity style={styles.withdrawButton} onPress={onWithdraw}>
                    <Text style={styles.withdrawButtonText}>Withdraw</Text>
                </TouchableOpacity>
            </View>

            <View style={styles.mapContainer}>
                <MapView
                    style={styles.map}
                    initialRegion={currentRegion}
                    onRegionChange={setCurrentRegion}
                >
                    {availableDeliveries.map((delivery) => (
                        <React.Fragment key={delivery.id}>
                            <Marker
                                coordinate={delivery.pickupLocation}
                                title="Pickup"
                                description={delivery.pickupLocation.address}
                                pinColor="green"
                            />
                            <Marker
                                coordinate={delivery.dropoffLocation}
                                title="Dropoff"
                                description={delivery.dropoffLocation.address}
                                pinColor="red"
                            />
                        </React.Fragment>
                    ))}
                </MapView>
            </View>

            <View style={styles.deliveriesList}>
                <Text style={styles.sectionTitle}>Available Deliveries</Text>
                <FlatList
                    data={availableDeliveries}
                    renderItem={renderDeliveryItem}
                    keyExtractor={(item) => item.id}
                    horizontal
                    showsHorizontalScrollIndicator={false}
                />
            </View>

            {selectedDelivery && (
                <View style={styles.deliveryDetails}>
                    <Text style={styles.detailsTitle}>Delivery Details</Text>
                    <Text>From: {selectedDelivery.pickupLocation.address}</Text>
                    <Text>To: {selectedDelivery.dropoffLocation.address}</Text>
                    <Text>Reward: KES {selectedDelivery.reward}</Text>
                    <TouchableOpacity
                        style={styles.acceptButton}
                        onPress={() => onDeliveryAccept(selectedDelivery)}
                    >
                        <Text style={styles.acceptButtonText}>Accept Delivery</Text>
                    </TouchableOpacity>
                </View>
            )}
        </SafeAreaView>
    );
};

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#f5f5f5',
    },
    header: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: 16,
        backgroundColor: '#fff',
        borderBottomWidth: 1,
        borderBottomColor: '#e0e0e0',
    },
    playerInfo: {
        flex: 1,
    },
    playerName: {
        fontSize: 18,
        fontWeight: 'bold',
    },
    balance: {
        fontSize: 16,
        color: '#4CAF50',
    },
    withdrawButton: {
        backgroundColor: '#2196F3',
        paddingHorizontal: 16,
        paddingVertical: 8,
        borderRadius: 8,
    },
    withdrawButtonText: {
        color: '#fff',
        fontWeight: 'bold',
    },
    mapContainer: {
        flex: 1,
        height: '50%',
    },
    map: {
        flex: 1,
    },
    deliveriesList: {
        padding: 16,
    },
    sectionTitle: {
        fontSize: 18,
        fontWeight: 'bold',
        marginBottom: 8,
    },
    deliveryItem: {
        backgroundColor: '#fff',
        padding: 16,
        borderRadius: 8,
        marginRight: 12,
        width: 250,
        shadowColor: '#000',
        shadowOffset: {
            width: 0,
            height: 2,
        },
        shadowOpacity: 0.25,
        shadowRadius: 3.84,
        elevation: 5,
    },
    deliveryTitle: {
        fontSize: 16,
        fontWeight: 'bold',
        marginBottom: 4,
    },
    rewardText: {
        color: '#4CAF50',
        fontWeight: 'bold',
    },
    deliveryDetails: {
        position: 'absolute',
        bottom: 0,
        left: 0,
        right: 0,
        backgroundColor: '#fff',
        padding: 16,
        borderTopLeftRadius: 16,
        borderTopRightRadius: 16,
        shadowColor: '#000',
        shadowOffset: {
            width: 0,
            height: -2,
        },
        shadowOpacity: 0.25,
        shadowRadius: 3.84,
        elevation: 5,
    },
    detailsTitle: {
        fontSize: 18,
        fontWeight: 'bold',
        marginBottom: 8,
    },
    acceptButton: {
        backgroundColor: '#4CAF50',
        padding: 16,
        borderRadius: 8,
        alignItems: 'center',
        marginTop: 16,
    },
    acceptButtonText: {
        color: '#fff',
        fontWeight: 'bold',
        fontSize: 16,
    },
}); 