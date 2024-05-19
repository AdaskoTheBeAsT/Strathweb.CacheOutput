using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebApi.OutputCache.V2.TimeAttributes;

namespace WebApi.OutputCache.V2.Demo
{
    [AutoInvalidateCacheOutput]
    public class Teams2Controller : ApiController
    {
        private static readonly List<Team> Teams = new List<Team>
        {
            new Team
            {
                Id = 1,
                League = "NHL",
                Name = "Leafs",
            },
            new Team
            {
                Id = 2,
                League = "NHL",
                Name = "Habs",
            },
        };

        [CacheOutput(ClientTimeSpan = 50, ServerTimeSpan = 50)]
        public IEnumerable<Team> Get()
        {
            return Teams;
        }

        [CacheOutputUntil(2014, 7, 20)]
        public Team GetById(int id)
        {
            var team = Teams.Find(i => i.Id == id);
            if (team == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return team;
        }

        public void Post(Team value)
        {
            if (!ModelState.IsValid)
            {
                using (var request = Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState))
                {
                    throw new HttpResponseException(request);
                }
            }

            Teams.Add(value);
        }

        public void Put(int id, Team value)
        {
            if (!ModelState.IsValid)
            {
                using (var request = Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState))
                {
                    throw new HttpResponseException(request);
                }
            }

            var team = Teams.Find(i => i.Id == id);
            if (team == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            team.League = value.League;
            team.Name = value.Name;
        }

        public void Delete(int id)
        {
            var team = Teams.Find(i => i.Id == id);
            if (team == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            Teams.Remove(team);
        }
    }
}
