namespace SmartPark.MWBot.Models
{
    public enum UserType { Base = 0, Premium = 1 }

    public enum ChargeRequestStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Cancelled = 3,
        Proposed = 4   // nuovo stato: in attesa di accettazione utente
    }

    public enum ParkingSessionStatus { Open = 0, Closed = 1 }

    public enum ChargeJobStatus { Queued = 0, Running = 1, Finished = 2, Aborted = 3 }
}
