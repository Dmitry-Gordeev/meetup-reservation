namespace MeetupReservation.Api.Notifications;

public static class EmailTemplates
{
    public static string RegistrationConfirmation(string participantName, string eventTitle, DateTime startAt, string? location)
    {
        var loc = !string.IsNullOrEmpty(location) ? location : "Онлайн";
        return $"""
            Здравствуйте, {participantName}!

            Вы успешно зарегистрировались на мероприятие «{eventTitle}».

            Дата и время: {startAt:dd.MM.yyyy HH:mm} (UTC)
            Место: {loc}

            Ждём вас!
            Meetup Reservation
            """;
    }

    public static string Reminder24Hours(string participantName, string eventTitle, DateTime startAt, string? location)
    {
        var loc = !string.IsNullOrEmpty(location) ? location : "Онлайн";
        return $"""
            Здравствуйте, {participantName}!

            Напоминаем: завтра состоится мероприятие «{eventTitle}».

            Дата и время: {startAt:dd.MM.yyyy HH:mm} (UTC)
            Место: {loc}

            До встречи!
            Meetup Reservation
            """;
    }

    public static string Reminder1Hour(string participantName, string eventTitle, DateTime startAt, string? location)
    {
        var loc = !string.IsNullOrEmpty(location) ? location : "Онлайн";
        return $"""
            Здравствуйте, {participantName}!

            Напоминаем: мероприятие «{eventTitle}» начнётся через 1 час.

            Дата и время: {startAt:dd.MM.yyyy HH:mm} (UTC)
            Место: {loc}

            Ждём вас!
            Meetup Reservation
            """;
    }

    public static string EventCancelled(string participantName, string eventTitle, DateTime startAt)
    {
        return $"""
            Здравствуйте, {participantName}!

            К сожалению, мероприятие «{eventTitle}», запланированное на {startAt:dd.MM.yyyy HH:mm}, отменено.

            Приносим извинения за неудобства.
            Meetup Reservation
            """;
    }

    public static string RegistrationCancelledByParticipant(string organizerName, string participantName, string participantEmail, string eventTitle, DateTime startAt)
    {
        return $"""
            Здравствуйте, {organizerName}!

            Участник {participantName} ({participantEmail}) отменил регистрацию на мероприятие «{eventTitle}» ({startAt:dd.MM.yyyy HH:mm}).

            Meetup Reservation
            """;
    }
}
