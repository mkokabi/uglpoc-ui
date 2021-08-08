import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { useEffect, useState } from "react";
import EndPoints from "../endpoints";

interface IAlarm {
  id: string,
  status: number
}

const Alarm = () => {
  const [connection, setConnection] = useState<HubConnection>();
  const [alarms, setAlarms] = useState<IAlarm[]>([]);

  useEffect(() => {
    const newConnection = new HubConnectionBuilder()
      .withUrl(EndPoints.MessagesHubUrl)
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(result => {
          console.log('Connected!');

          connection.on('ReceiveMessage', message => {
            console.log(message);
            if (message.body === "alarmchange") {
              let apiUrl = EndPoints.AlarmsApiUrl;

              fetch(apiUrl)
                .then(res => res.json())
                .then(
                  (result) => {
                    console.log(result);
                    var alarms = result as IAlarm[];
                    if (result.length === 0) {
                      // setError({message: "Empty response"});
                      return;
                    }
                    setAlarms(alarms);
                  },
                  (error) => {
                    // setIsLoaded(true);
                    // setError(error);
                  });
            }
          })
        })
        .catch(e => console.log('Connection failed: ', e));
    }
  }
    , [connection]);

  return (
    <>
      <ul className="Column">
        {alarms.map(item =>
          <li key={item.id}>
            {item.id}-{item.status}
          </li>
        )}
      </ul>
    </>)
};

export default Alarm;