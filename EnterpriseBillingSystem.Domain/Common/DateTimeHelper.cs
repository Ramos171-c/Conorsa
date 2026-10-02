using System;

namespace EnterpriseBillingSystem.Domain.Common;

public static class DateTimeHelper
{
    // Zona Horaria para La Unión / Nicaragua / Centroamérica (UTC-6)
    // No tiene horario de verano (DST), siempre UTC-6.
    private static readonly TimeZoneInfo LocalTimeZone = ResolveTimeZone();

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Managua");
            }
            catch
            {
                return TimeZoneInfo.CreateCustomTimeZone("CentralAmerica", TimeSpan.FromHours(-6), "Central America Time", "Central America Time");
            }
        }
    }

    /// <summary>
    /// Obtiene la fecha y hora local actual en Nicaragua / Centroamérica (UTC-6).
    /// </summary>
    public static DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LocalTimeZone);

    /// <summary>
    /// Obtiene la fecha de hoy a las 00:00:00 en hora local (UTC-6).
    /// El día inicia a las 00:00 (12:00 AM medianoche) y cierra a las 23:59:59 (11:59:59 PM).
    /// </summary>
    public static DateTime Today => LocalNow.Date;

    /// <summary>
    /// Convierte una fecha a hora local de Centroamérica si viene en formato UTC.
    /// </summary>
    public static DateTime ToLocalTime(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dt, LocalTimeZone);
        }

        return dt;
    }
}
