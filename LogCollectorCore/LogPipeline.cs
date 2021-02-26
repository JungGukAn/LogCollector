using System;
using System.Linq;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace LogCollectorCore
{
    public class LogPipeline
    {
        static readonly string zeroOrAllMatchPattern = "**";
        static readonly string oneMatchPattern = "*";

        List<string> patternTokens;

        public readonly ImmutableList<Func<TagLog, bool>> Filters;
        public readonly ImmutableList<Action<Tag, List<dynamic>>> Outputs;

        protected LogPipeline(List<string> patternTokens, List<Func<TagLog, bool>> filters, List<Action<Tag, List<dynamic>>> outputs)
        {
            this.patternTokens = patternTokens.ToList();

            Filters = filters.ToImmutableList();
            Outputs = outputs.ToImmutableList();
        }

        public bool IsMatch(List<string> tags)
        {
            int maxCount = tags.Count > patternTokens.Count ? patternTokens.Count : tags.Count;

            for (int i = 0; i < maxCount; i++)
            {
                var patternToken = patternTokens[i];

                if (patternToken.Equals(zeroOrAllMatchPattern))
                    return true;

                if (patternToken.Equals(oneMatchPattern))
                    continue;

                if (patternToken.Equals(tags[i], StringComparison.OrdinalIgnoreCase))
                    continue;

                return false;
            }

            if (patternTokens.Count == tags.Count)
            {
                return true;
            }

            if (patternTokens.Count > tags.Count)
            {
                var nextPattern = patternTokens[maxCount];
                if (nextPattern.Equals(zeroOrAllMatchPattern))
                {
                    return true;
                }

                return false;
            }

            return false;
        }

        public class Builder
        {
            List<string> patternTokens;

            List<Func<TagLog, bool>> filters = new List<Func<TagLog, bool>>();
            List<Action<Tag, List<dynamic>>> outputs = new List<Action<Tag, List<dynamic>>>();

            public Builder(string pattern)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    pattern = zeroOrAllMatchPattern;
                }

                patternTokens = pattern.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList();

                if (string.IsNullOrWhiteSpace(pattern))
                {
                    throw new ArgumentNullException("'pattern' can not be null or empty.");
                }

                if (patternTokens.Count == 0)
                {
                    throw new ArgumentException();
                }
            }

            public Builder Add(Func<TagLog, bool> filter)
            {
                if (filter == null)
                {
                    throw new ArgumentNullException("filter can not be null.");
                }

                filters.Add(filter);
                return this;
            }

            public Builder Add(Action<TagLog> filter)
            {
                if (filter == null)
                {
                    throw new ArgumentNullException("filter can not be null.");
                }

                filters.Add((tagLog)=>
                {
                    filter.Invoke(tagLog);

                    if (string.IsNullOrWhiteSpace(tagLog.Tag))
                    {
                        return false;
                    }

                    return true;
                });

                return this;
            }

            public Builder Add(Action<Tag, List<dynamic>> output)
            {
                outputs.Add(output);
                return this;
            }

            public LogPipeline ToPipeline()
            {
                return new LogPipeline(patternTokens, filters, outputs);
            }
        }
    }
}
