using Serilog.Sinks.Oracle.Batch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Serilog.Sinks.Burst;

namespace Serilog.Sinks.Oracle.UnitTests.Batch
{
    public class BatchLoggerConfigurationTests
    {
        [Fact(DisplayName = "Should throw exception when calling CreateSink without settings (using Periodic)")]
        public void Should_throw_exception_when_calling_CreateSink_without_settings_using_periodic()
        {
            var exception = Assert.Throws<NotSupportedException>(() =>
                new BatchLoggerConfiguration()
                    .UsePeriodicBatch()
                    .CreateSink());

            exception.Message
                .Should()
                .BeEquivalentTo("Use the method WithSettings before calling CreateSink.");
        }

        [Fact(DisplayName = "Should throw exception when calling CreateSink with two batch sinks")]
        public void Should_throw_exception_when_calling_CreateSink_with_two_batch_sinks()
        {
            var exception = Assert.Throws<NotSupportedException>(() =>
                new BatchLoggerConfiguration()
                    .UsePeriodicBatch()
                    .UseBurstBatch()
                    .CreateSink());

            exception.Message
                .Should()
                .BeEquivalentTo("Can not use more than one Batch Configuration.");
        }

        [Fact(DisplayName = "Should throw exception when calling CreateSink without settings (using Burst)")]
        public void Should_throw_exception_when_calling_CreateSink_without_settings_using_burst()
        {
            var exception = Assert.Throws<NotSupportedException>(() =>
                new BatchLoggerConfiguration()
                    .UseBurstBatch()
                    .CreateSink());

            exception.Message
                .Should()
                .BeEquivalentTo("Use the method WithSettings before calling CreateSink.");
        }

        [Fact(DisplayName = "Should throw exception when calling CreateSink without a batch sink configured")]
        public void Should_throw_exception_when_calling_CreateSink_without_a_batch_sink_configured()
        {
            var exception = Assert.Throws<NotSupportedException>(() =>
                new BatchLoggerConfiguration()
                    .WithSettings("this is a fake connection")
                    .CreateSink());

            exception.Message
                .Should()
                .BeEquivalentTo("Use the method UseBurstBatch or UsePeriodicBatch before calling CreateSink.");
        }

        [Fact(DisplayName = "Should create the sink when calling with settings and a batch sink (using burst)")]
        public void Should_create_the_sink_when_calling_with_settings_and_a_batch_sink_using_burst()
        {
            var sink = new BatchLoggerConfiguration()
                .WithSettings("this is a fake connection")
                .UseBurstBatch()
                .CreateSink();

            sink.Should().NotBeNull();
            sink.Should().BeOfType<BurstSink>();
        }

        [Fact(DisplayName = "Should create the sink when calling with settings and a batch sink (using periodic)")]
        public void Should_create_the_sink_when_calling_with_settings_and_a_batch_sink_using_periodic()
        {
            var sink = new BatchLoggerConfiguration()
                .WithSettings("this is a fake connection")
                .UsePeriodicBatch()
                .CreateSink();

            sink.Should().NotBeNull();
            sink.Should().BeOfType<PeriodicBatchingSinkWrapper>();
        }
    }
}

