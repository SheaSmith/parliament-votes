using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels
{
    public class Meta
    {
        public int Length { get; set; }

        public object Content { get; set; }

        public Meta(int length, object content)
        {
            Length = length;
            Content = content;
        }
    }
}
