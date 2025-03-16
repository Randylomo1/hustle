export interface Player {
    id: string;
    username: string;
    balance: number;
    experience: number;
    level: number;
    vehicles: Vehicle[];
    completedDeliveries: number;
    rating: number;
    mpesaPhone: string;
}

export interface Vehicle {
    id: string;
    type: VehicleType;
    name: string;
    speed: number;
    capacity: number;
    fuelEfficiency: number;
    maintenance: number;
    cost: number;
    isLocked: boolean;
}

export enum VehicleType {
    MOTORCYCLE = 'MOTORCYCLE',
    TUKTUK = 'TUKTUK',
    CAR = 'CAR',
    VAN = 'VAN',
    TRUCK = 'TRUCK'
}

export interface Delivery {
    id: string;
    pickupLocation: Location;
    dropoffLocation: Location;
    distance: number;
    reward: number;
    timeLimit: number;
    status: DeliveryStatus;
    difficulty: number;
    requiredVehicleType: VehicleType;
}

export interface Location {
    latitude: number;
    longitude: number;
    address: string;
}

export enum DeliveryStatus {
    AVAILABLE = 'AVAILABLE',
    ACCEPTED = 'ACCEPTED',
    IN_PROGRESS = 'IN_PROGRESS',
    COMPLETED = 'COMPLETED',
    FAILED = 'FAILED'
}

export interface Transaction {
    id: string;
    playerId: string;
    amount: number;
    type: TransactionType;
    status: TransactionStatus;
    timestamp: Date;
    mpesaReference?: string;
}

export enum TransactionType {
    EARNING = 'EARNING',
    WITHDRAWAL = 'WITHDRAWAL',
    VEHICLE_PURCHASE = 'VEHICLE_PURCHASE',
    MAINTENANCE = 'MAINTENANCE'
}

export enum TransactionStatus {
    PENDING = 'PENDING',
    COMPLETED = 'COMPLETED',
    FAILED = 'FAILED'
} 