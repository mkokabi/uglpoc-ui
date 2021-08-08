import React from 'react';
import './App.css';
import Alarm from './components/Alarm';

function App() {
  const M = 5;
  const elems = Array.from(Array(M), (_, index) => index + 1); 
  return (
        <div className="Row">
        {elems.map(() => (
          <Alarm />
        ))}</div>
  );
}

export default App;
