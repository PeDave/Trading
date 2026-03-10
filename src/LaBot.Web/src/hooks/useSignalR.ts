import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '@/stores/authStore';

export function useSignalR(hubUrl: string) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const accessToken = useAuthStore((s) => s.accessToken);

  useEffect(() => {
    if (!accessToken) return;
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => accessToken })
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;
    connection
      .start()
      .then(() => setIsConnected(true))
      .catch(console.error);

    connection.onclose(() => setIsConnected(false));
    connection.onreconnected(() => setIsConnected(true));
    connection.onreconnecting(() => setIsConnected(false));

    return () => {
      connection.stop();
    };
  }, [hubUrl, accessToken]);

  return { connection: connectionRef.current, isConnected };
}
