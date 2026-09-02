using System;

namespace adan
{
    public class PrayerTime
    {
        public string Name;
        public DateTime Time;
        public PrayerTime(string name, DateTime time)
        { Name = name; Time = time; }
    }

    public class PrayerCalculator
    {
        private const double LAT = 32.8667;
        private const double LNG = -6.5667;
        private const double ALTITUDE = 465.0;
        private const double FAJR_ANGLE = 18.0;
        private const double ISHA_ANGLE = 17.0;

        private const double CORR_FAJR = -5.0;
        private const double CORR_DHUHR = 5.0;
        private const double CORR_ASR = 0.0;
        private const double CORR_MAGHRIB = 5.0;
        private const double CORR_ISHA = 0.0;

        private static double ToRad(double d) { return d * Math.PI / 180.0; }
        private static double ToDeg(double r) { return r * 180.0 / Math.PI; }
        private static double FixH(double h)
        { h = h % 24.0; return h < 0.0 ? h + 24.0 : h; }

        private static double JulianDay(int y, int m, int d)
        {
            if (m <= 2) { y--; m += 12; }
            double A = Math.Floor(y / 100.0);
            double B = 2.0 - A + Math.Floor(A / 4.0);
            return Math.Floor(365.25 * (y + 4716.0))
                 + Math.Floor(30.6001 * (m + 1.0)) + d + B - 1524.5;
        }

        private static void SunPos(double jd, out double dec, out double EqT)
        {
            double D = jd - 2451545.0;
            double g = ToRad(357.529 + 0.98560028 * D);
            double q = 280.459 + 0.98564736 * D;
            double L = ToRad(q + 1.915 * Math.Sin(g) + 0.020 * Math.Sin(2.0 * g));
            double e = ToRad(23.439 - 0.00000036 * D);
            double RA = ToDeg(Math.Atan2(Math.Cos(e) * Math.Sin(L),
                                          Math.Cos(L))) / 15.0;
            dec = Math.Asin(Math.Sin(e) * Math.Sin(L));
            EqT = q / 15.0 - FixH(RA);
        }

        private static double AT(double noon, double dec,
                                  double latR, double angle, int dir)
        {
            double v = (-Math.Sin(ToRad(angle))
                        - Math.Sin(dec) * Math.Sin(latR))
                      / (Math.Cos(dec) * Math.Cos(latR));
            if (v < -1.0) v = -1.0;
            if (v > 1.0) v = 1.0;
            return noon + dir * ToDeg(Math.Acos(v)) / 15.0;
        }

        private static PrayerTime Make(DateTime date, string name,
                                        double hours, int adjMinutes)
        {
            // Apply manual adjustment in minutes
            hours += adjMinutes / 60.0;
            hours = FixH(hours);
            int h = (int)hours;
            int mn = (int)Math.Floor((hours - h) * 60.0 + 0.5);
            if (mn == 60) { h++; mn = 0; }
            return new PrayerTime(name,
                new DateTime(date.Year, date.Month, date.Day,
                             h % 24, mn, 0));
        }

        public PrayerTime[] Calculate(DateTime date)
        {
            double dec, EqT;
            SunPos(JulianDay(date.Year, date.Month, date.Day),
                   out dec, out EqT);

            double tz = (double)AppSettings.TimeOffsetHours;
            double latR = ToRad(LAT);
            double ac = (0.0347 * Math.Sqrt(ALTITUDE)) / 60.0;
            double noon = 12.0 - LNG / 15.0 - EqT;
            double asrAlt = ToDeg(Math.Atan(
                1.0 / (1.0 + Math.Tan(Math.Abs(latR - dec)))));

            // Load per-prayer manual adjustments
            int[] adj = AppSettings.GetAdjustments();

            return new PrayerTime[]
            {
                Make(date, "Fajr",
                     AT(noon,dec,latR,FAJR_ANGLE,-1)+CORR_FAJR/60.0+tz,   adj[0]),
                Make(date, "Sunrise",
                     AT(noon,dec,latR,0.833,-1)-ac+tz,                      adj[1]),
                Make(date, "Dhuhr",
                     noon+CORR_DHUHR/60.0+tz,                              adj[2]),
                Make(date, "Asr",
                     AT(noon,dec,latR,-asrAlt,+1)+CORR_ASR/60.0+tz,        adj[3]),
                Make(date, "Maghrib",
                     AT(noon,dec,latR,0.833,+1)+ac+2.0/60.0+CORR_MAGHRIB/60.0+tz, adj[4]),
                Make(date, "Isha",
                     AT(noon,dec,latR,ISHA_ANGLE,+1)+CORR_ISHA/60.0+tz,    adj[5]),
            };
        }
    }
}