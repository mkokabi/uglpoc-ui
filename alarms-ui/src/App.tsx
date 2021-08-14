import React from 'react';
import './App.css';
import Alarm from './components/Alarm';

function App() {
  const M = 4;
  const elems = Array.from(Array(M), (_, index) => String.fromCharCode(65 + index)); 
  return (
        <div className="Row">
        {elems.map((trainName) => (
          <Alarm key={trainName} trainName={trainName} />
        ))}</div>
  );
}

export default App;
