#!/bin/bash

PORT=5070

echo "Checking port $PORT..."

sudo fuser -k "$PORT/tcp" 2>/dev/null

echo "Starting Clinic Appointment application..."
echo "URL: http://localhost:$PORT"

dotnet build && dotnet watch --urls "http://localhost:$PORT"