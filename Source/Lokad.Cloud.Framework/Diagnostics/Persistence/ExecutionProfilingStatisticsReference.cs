﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage;
using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics.Persistence
{
	internal class ExecutionProfilingStatisticsReference : BlobReference<ExecutionProfilingStatistics>
	{
		public override string ContainerName
		{
			get { return "lokad-cloud-diag-profile"; }
		}

		[UsedImplicitly, Rank(0)]
		public readonly string TimeSegment;

		[UsedImplicitly, Rank(1)]
		public readonly string ContextName;

		public ExecutionProfilingStatisticsReference(string timeSegment, string contextName)
		{
			TimeSegment = timeSegment;
			ContextName = contextName;
		}

		public static ExecutionProfilingStatisticsReference New(string timeSegment, string contextName)
		{
			return new ExecutionProfilingStatisticsReference(timeSegment, contextName);
		}

        public static ExecutionProfilingStatisticsReference GetPrefix()
		{
			return new ExecutionProfilingStatisticsReference(null, null);
		}

        public static ExecutionProfilingStatisticsReference GetPrefix(string timeSegment)
		{
			return new ExecutionProfilingStatisticsReference(timeSegment, null);
		}
	}
}