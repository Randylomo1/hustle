# Nairobi Hustle

A high-performance multiplayer game built with Unity, featuring realistic graphics and secure payment integration.

## Features

### Advanced Graphics
- Ray tracing support
- DLSS and FSR optimization
- HDRI lighting
- Screen Space Reflections
- Advanced post-processing effects
- Dynamic weather system
- Day/night cycle
- Realistic crowd and traffic systems

### Payment System
- Multiple payment gateways:
  - M-Pesa
  - PayPal
  - Stripe
  - Cryptocurrency
- Secure transaction processing
- Automatic failover
- Load balancing
- Transaction queuing
- Retry mechanisms
- Comprehensive error handling

### Multiplayer System
- 64 tick rate servers
- Regional server locations:
  - East Africa (Nairobi)
  - South Africa (Johannesburg)
  - West Africa (Lagos)
- Client-side prediction
- Server reconciliation
- Entity interpolation
- Lag compensation
- State compression
- Packet encryption

### Security Features
- Memory protection
- Anti-cheat system
- DDoS protection
- Secure authentication
- Rate limiting
- Input validation
- State verification

## Requirements

- Unity 2022.3 or later
- .NET 6.0 or later
- Mirror Networking
- HDRP (High Definition Render Pipeline)

## Installation

1. Clone the repository:
```bash
git clone https://github.com/Randylomo1/nairobihustle.git
```

2. Open the project in Unity Hub

3. Install required dependencies:
   - Mirror Networking
   - HDRP packages
   - Post-processing packages

4. Configure payment gateway credentials:
   - Create a `PaymentConfig.json` file
   - Add your API keys for each payment gateway
   - Place in the appropriate location (see documentation)

5. Build and run the project

## Configuration

### Payment Gateways
- Configure API keys in `PaymentConfig.json`
- Set transaction limits
- Configure retry policies
- Set timeout values

### Network Settings
- Adjust tick rate
- Configure regional servers
- Set maximum players
- Adjust latency compensation

### Graphics Settings
- Enable/disable ray tracing
- Configure DLSS/FSR
- Adjust post-processing
- Set weather parameters

## Security

- All API keys must be stored securely
- Payment credentials must be encrypted
- Network traffic is encrypted
- Anti-cheat system is enabled by default
- Regular security updates are required

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is proprietary and confidential. All rights reserved.

## Support

For support, please contact the development team at [your-email@example.com] 