
using ExamQAMovieCatalog.Models;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Net;
using System.Text.Json;



namespace ExamQAMovieCatalog
{
    [TestFixture]
    public class Tests
    {
        private RestClient _client;
        private static string lastCreatedMovieId;

        private const string BaseUrl = "http://144.91.123.158:5000";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJlZTE2ZGYyMi05NjFhLTRlZTItODI1Ni05NTI0NzNlM2I1NWEiLCJpYXQiOiIwNC8xOC8yMDI2IDA2OjAzOjA2IiwiVXNlcklkIjoiZjZkOWI2NDQtZGI5OS00MGVmLTYxZTgtMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJ0YW5pYUBzb2Z0dW5pLmNvbSIsIlVzZXJOYW1lIjoiVGFuaWEiLCJleHAiOjE3NzY1MTM3ODYsImlzcyI6Ik1vdmllQ2F0YWxvZ19BcHBfU29mdFVuaSIsImF1ZCI6Ik1vdmllQ2F0YWxvZ19XZWJBUElfU29mdFVuaSJ9.rkrs16FP-mYb27iLjt8UoURtHnsTMt0ORONjAAs2ofo";
        private const string LoginEmail = "tania@softuni.com";
        private const string LoginPassword = "tania123";
        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }
            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            this._client = new RestClient(options);
        }
        private string GetJwtToken(string email, string password)
        {
            var client = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("token").GetString();
                if (string.IsNullOrWhiteSpace(token))
                     {
                        throw new InvalidOperationException("Token not found in the response");
                      }
                    
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
}
        [Order(1)]
        [Test]
        public void CreateMovie_WithRequiredFields_ShouldReturnSuccess()
        {
            MovieDTO newMovie = new MovieDTO
            {
                Title = "New Movie",
                Description = "This is a new movie.",
            };

            RestRequest request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(newMovie);

            RestResponse response = this._client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            Assert.That(createResponse.Movie, Is.Not.Null, "Expected a movie object in the response");
            Assert.That(createResponse.Movie.Id, Is.Not.Null.And.Not.Empty, "Expected the created movie to have a non-empty Id");
            Assert.That(createResponse.Msg, Is.EqualTo("Movie created successfully!"), "Expected success message in the response");
            //if (createResponse.Movie.Id != null)
            //{
            //    lastCreatedMovieId = createResponse.Movie.Id;
            //}
            lastCreatedMovieId = createResponse.Movie.Id;
           
        }

        [Order(2)]
        [Test]
        public void EditCreatedMovie_ShouldReturnSuccess()
        {
         
            MovieDTO updatedMovie = new MovieDTO
            {
                Title = "Updated Movie Title",
                Description = "This is an updated description."
            };
            RestRequest request = new RestRequest($"/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", lastCreatedMovieId);
            request.AddJsonBody(updatedMovie);

            RestResponse response = this._client.Execute(request);
            var editedResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);


            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            Assert.That(editedResponse.Msg, Is.EqualTo("Movie edited successfully!"));

        }

        [Order(3)]
        [Test]
        public void GetAllMovie_ShouldReturnSuccess()
        {
            RestRequest request = new RestRequest("/api/Catalog/All", Method.Get);
            RestResponse response = this._client.Execute(request);
            var movies = JsonSerializer.Deserialize<List<MovieDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            Assert.That(movies, Is.Not.Null);
            Assert.That(movies, Is.Not.Empty);
        }

        [Order(4)]
        [Test]
        public void DeleteCreatedMovie_ShouldReturnSuccess()
        {
            RestRequest request = new RestRequest($"/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", lastCreatedMovieId);
            RestResponse response = this._client.Execute(request);
            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            Assert.That(deleteResponse.Msg, Is.EqualTo("Movie deleted successfully!"), "Expected success message in the response");
        }

        [Order(5)]
        [Test]
        public void CreateMovie_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            MovieDTO newMovie = new MovieDTO
            {
                Title = "",
                Description = "This movie has no title.",
            };
            RestRequest request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(newMovie);
            RestResponse response = this._client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request");
        }

        [Order(6)]
        [Test]
        public void EditNonexistentMovie_ShouldReturnBadRequest()
        {
            var editNonExistingMovie = new MovieDTO
            {
                Title = "Nonexistent Movie",
                Description = "This movie does not exist."
            };
            RestRequest request = new RestRequest($"/api/Movie/Edit", Method.Put);
            var nonExistingMovieid = "8888";
            request.AddQueryParameter("movieId", nonExistingMovieid);
            request.AddJsonBody(editNonExistingMovie);
            RestResponse response = this._client.Execute(request);
            var readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request");
            Assert.That(readyResponse.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Order(7)]
        [Test]
        public void DeleteNonexistentMovie_ShouldReturnBadRequest()
        {
            RestRequest request = new RestRequest($"/api/Movie/Delete", Method.Delete);
            var nonExistingMovieid = "8888";
            request.AddQueryParameter("movieId", nonExistingMovieid);
            RestResponse response = this._client.Execute(request);
            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request");
            Assert.That(deleteResponse.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this._client?.Dispose();
        }
    }
}