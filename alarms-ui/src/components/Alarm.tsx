import { Stack } from "@fluentui/react/lib/Stack";
import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { useEffect, useState } from "react";
import EndPoints from "../endpoints";

interface IAlarm {
  id: string;
  status: number;
}
interface Error {
  message: string;
}

interface IAlarmComponentProps {
  trainName: string;
}

const Alarm = (props: IAlarmComponentProps) => {
  const [connection, setConnection] = useState<HubConnection>();
  const [alarms, setAlarms] = useState<IAlarm[]>([]);
  const [lastMessageTime, setlastMessageTime] = useState("");
  const [error, setError] = useState<Error>();

  useEffect(() => {
    const newConnection = new HubConnectionBuilder()
      .withUrl(EndPoints.MessagesHubUrl)
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, []);

  const checkTrainName = (body: string, trainName: string): boolean => {
    if (body.startsWith("alarmchange_")) {
      const trainsNames = body.replace("alarmchange_", "");
      if (trainsNames) {
        return (
          trainsNames.split(",").find((tn) => tn === trainName) !== undefined
        );
      }
    }
    return false;
  };

  useEffect(() => {
    if (connection) {
      connection
        .start()
        .then((result) => {
          console.log("Connected!");

          connection.on("ReceiveMessage", (message) => {
            const trainName = props.trainName;
            if (checkTrainName(message.body, trainName)) {
              console.log(message);
              setlastMessageTime(new Date(message.time).toLocaleTimeString());
              let apiUrl = `${EndPoints.AlarmsApiUrl}?trainName=${trainName}`;

              fetch(apiUrl)
                .then((res) => res.json())
                .then(
                  (result) => {
                    console.log(result);
                    var alarms = result as IAlarm[];
                    if (result.length === 0) {
                      setError({ message: "Empty response" });
                      return;
                    }
                    setAlarms(alarms);
                  },
                  (error) => {
                    // setIsLoaded(true);
                    setError(error);
                  }
                );
            }
          });
        })
        .catch((e) => console.log("Connection failed: ", e));
    }
  }, [connection, props.trainName]);

  return (
    <Stack>
      <span>
        {props.trainName} {lastMessageTime}
      </span>
      {error ? <span>{error.message}</span> : ""}
      <ul className="Column">
        {alarms.map((item) => (
          <li key={item.id}>
            {item.id}-{item.status}
          </li>
        ))}
      </ul>
    </Stack>
  );
};

export default Alarm;
