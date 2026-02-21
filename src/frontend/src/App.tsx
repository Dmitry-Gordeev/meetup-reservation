import config from './config.json'

function App() {
  const apiBaseUrl = config.apiBaseUrl

  return (
    <div>
      <h1>Meetup Reservation</h1>
      <p>API: {apiBaseUrl}</p>
    </div>
  )
}

export default App
