using Prometheus;

namespace Shared.Telemetry;

public static class TaskMetrics
{
    private static readonly Gauge Pending = Metrics.CreateGauge(
        "tasks_pending_total",
        "Number of tasks that are waiting or being processed");

    private static readonly Counter Failed = Metrics.CreateCounter(
        "tasks_failed_total",
        "Total number of failed tasks");

    private static readonly Histogram Duration = Metrics.CreateHistogram(
        "job_duration_seconds",
        "Task processing time in seconds",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.05, 2, 10)
        });

    public static void TaskReceived() => Pending.Inc();

    public static void TaskCompleted(double seconds)
    {
        Pending.Dec();
        Duration.Observe(seconds);
    }

    public static void TaskFailed()
    {
        Pending.Dec();
        Failed.Inc();
    }
}