using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Votes
{
    /// <summary>
    /// As a voice vote doesn't have any extra info associated with it we don't store anything in here, but we want to keep them in our database
    /// </summary>
    public class VoiceVote : Vote
    {
    }
}
