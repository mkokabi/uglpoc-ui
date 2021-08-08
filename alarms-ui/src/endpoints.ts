const EndPoints = {
  AlarmsApiUrl: (process.env.REACT_APP_BACKEND_API_URL ?? '') +"/Alarm",
  MessagesHubUrl: (process.env.REACT_APP_BACKEND_API_URL ?? '') + '/hubs/messages'
 }
 
 export default EndPoints;