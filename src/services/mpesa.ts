import axios from 'axios';
import { Transaction, TransactionStatus, TransactionType } from '../types';

export class MpesaService {
    private readonly baseUrl: string;
    private readonly consumerKey: string;
    private readonly consumerSecret: string;
    private readonly passkey: string;
    private readonly shortcode: string;

    constructor() {
        // These would come from environment variables in production
        this.baseUrl = 'https://sandbox.safaricom.co.ke';
        this.consumerKey = 'YOUR_CONSUMER_KEY';
        this.consumerSecret = 'YOUR_CONSUMER_SECRET';
        this.passkey = 'YOUR_PASSKEY';
        this.shortcode = 'YOUR_SHORTCODE';
    }

    private async getAccessToken(): Promise<string> {
        const auth = Buffer.from(`${this.consumerKey}:${this.consumerSecret}`).toString('base64');
        try {
            const response = await axios.get(`${this.baseUrl}/oauth/v1/generate?grant_type=client_credentials`, {
                headers: {
                    Authorization: `Basic ${auth}`,
                },
            });
            return response.data.access_token;
        } catch (error) {
            console.error('Error getting access token:', error);
            throw new Error('Failed to get access token');
        }
    }

    async initiatePayment(phoneNumber: string, amount: number): Promise<Transaction> {
        try {
            const accessToken = await this.getAccessToken();
            const timestamp = new Date().toISOString().replace(/[^0-9]/g, '').slice(0, -3);
            const password = Buffer.from(`${this.shortcode}${this.passkey}${timestamp}`).toString('base64');

            const response = await axios.post(
                `${this.baseUrl}/mpesa/stkpush/v1/processrequest`,
                {
                    BusinessShortCode: this.shortcode,
                    Password: password,
                    Timestamp: timestamp,
                    TransactionType: 'CustomerPayBillOnline',
                    Amount: amount,
                    PartyA: phoneNumber,
                    PartyB: this.shortcode,
                    PhoneNumber: phoneNumber,
                    CallBackURL: 'https://your-callback-url.com/mpesa/callback',
                    AccountReference: 'Nairobi Hustle',
                    TransactionDesc: 'Game Payment',
                },
                {
                    headers: {
                        Authorization: `Bearer ${accessToken}`,
                    },
                }
            );

            return {
                id: response.data.CheckoutRequestID,
                playerId: phoneNumber, // This should be the actual player ID in production
                amount: amount,
                type: TransactionType.EARNING,
                status: TransactionStatus.PENDING,
                timestamp: new Date(),
                mpesaReference: response.data.CheckoutRequestID,
            };
        } catch (error) {
            console.error('Error initiating payment:', error);
            throw new Error('Failed to initiate payment');
        }
    }

    async processWithdrawal(phoneNumber: string, amount: number): Promise<Transaction> {
        try {
            const accessToken = await this.getAccessToken();
            
            // Implementation for B2C (Business to Customer) payment
            // This would require additional API endpoints and permissions from Safaricom
            
            return {
                id: Date.now().toString(),
                playerId: phoneNumber, // This should be the actual player ID in production
                amount: amount,
                type: TransactionType.WITHDRAWAL,
                status: TransactionStatus.PENDING,
                timestamp: new Date(),
            };
        } catch (error) {
            console.error('Error processing withdrawal:', error);
            throw new Error('Failed to process withdrawal');
        }
    }

    async verifyTransaction(transactionId: string): Promise<TransactionStatus> {
        try {
            const accessToken = await this.getAccessToken();
            
            // Implementation for transaction status check
            // This would use the query transaction status API from Safaricom
            
            return TransactionStatus.COMPLETED;
        } catch (error) {
            console.error('Error verifying transaction:', error);
            throw new Error('Failed to verify transaction');
        }
    }
} 