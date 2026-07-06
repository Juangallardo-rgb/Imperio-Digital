import { useEffect, useRef } from "react";
import {
  getRealtimeConnection,
  startRealtimeConnection,
} from "../realtime/realtimeConnection";

function useRealtimeRefresh(
  eventNames,
  refreshCallback,
  pollingMilliseconds = 15000
) {
  const refreshRef = useRef(refreshCallback);

  useEffect(() => {
    refreshRef.current = refreshCallback;
  }, [refreshCallback]);

  useEffect(() => {
    let isMounted = true;
    const connection = getRealtimeConnection();

    const handleRefresh = (...eventArguments) => {
      if (!isMounted) {
        return;
      }

      Promise.resolve(
        refreshRef.current(...eventArguments)
      ).catch((error) => {
        console.error(
          "Error actualizando datos en tiempo real:",
          error
        );
      });
    };

    eventNames.forEach((eventName) => {
      connection.on(eventName, handleRefresh);
    });

    const handleVisibilityChange = () => {
      if (document.visibilityState === "visible") {
        handleRefresh();
      }
    };

    const handleFocus = () => {
      handleRefresh();
    };

    const handleReconnect = () => {
      handleRefresh();
    };

    document.addEventListener(
      "visibilitychange",
      handleVisibilityChange
    );

    window.addEventListener("focus", handleFocus);

    window.addEventListener(
      "imperio:realtime-reconnected",
      handleReconnect
    );

    void startRealtimeConnection();

    const pollingId = window.setInterval(
      handleRefresh,
      pollingMilliseconds
    );

    return () => {
      isMounted = false;

      eventNames.forEach((eventName) => {
        connection.off(eventName, handleRefresh);
      });

      document.removeEventListener(
        "visibilitychange",
        handleVisibilityChange
      );

      window.removeEventListener("focus", handleFocus);

      window.removeEventListener(
        "imperio:realtime-reconnected",
        handleReconnect
      );

      window.clearInterval(pollingId);
    };
  }, [eventNames, pollingMilliseconds]);
}

export default useRealtimeRefresh;