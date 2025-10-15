using VectorRagDemo.Models;
using Google.Apis.Auth.OAuth2;

namespace VectorRagDemo.BLL
{
    public static class ConnectionProcessor
    {
        public static async Task<string> GetAuthenticationToken() 
        {
            string omgeving = "DEV";
            string jsonToken = string.Empty;

            switch (omgeving)
            {
                case "DEV":
                    jsonToken = @"{
                          ""type"": ""service_account"",
                          ""project_id"": ""plenary-song-384814"",
                          ""private_key_id"": ""558afd9187538fbea16871adbb734f536ad00d27"",
                          ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQCqzp4Jdpc4OWcC\nErJFansMVbrJ+4LkeIr40pwlaKLhyAZOZzl+oSh333DoT/94KCUVL46txBQ2thXP\nRpeewR2rCRpAd0/KnQRgtJHCyXyAUV1vZIVY+zLcVluV9jmw0EqGWOJRBdLTaL8I\nRfjMYC6x0ZvJNwjre///pF2nxlUgZ527xBZCtD6R0cl0Tl5StT48Cy1v2BQaJYcm\nT4K1eZ6EID/iKMgTdOgexXu4yyhSJs1I/ED995Fz+Dlwm+PrhkQtBgPZE7MTH6Ja\nThN6/lUCDarTUmPwhyb8vTX2B/Q+CCzUzdHkR/vWQe8RiePouI+hspDxyCaSn8Ir\nGDD0mp57AgMBAAECggEASEeY/t4tTcCUw37P66oMmgKpQZHqKO6NuI+/PeFSlALG\nEGWEIf7mlht6twQrUliCoL14PjYsa56QCh+QR8Dm4hq+/iq/HDlnwdiHmgsTYWWN\nCXdbKnVaZ30v0nzYmub5snJoiurQ9V9s/35Es4+8EslliDd+0c+uXCrc6wN1wHkH\nG/JNHboVqfeKm0eiNazsktBxr6ZPU4KHoCCW6ypYoLgGWVQQlfha/2FY59SUh+gb\ni+3uK7sY65S7zYwqTKFudgAPDu04Ye/jvejaSIMl9ojwoialFtfxG22HGbZXLqzc\n0bVyfOEeGwdOwDXoKPY7rHhXQGbZ+2VzbYG9XRtBaQKBgQDfaTmG5wyXo04u9A52\n+ypf+Nw/HZnIJwKQO7UQWxjSD63aCoZ9B3OZRLuKtRoLj2xDaZloZKt2ejD6QP92\nyzkHX0Jw0RICVjGNugHsVhd9SUgZuzipva7sIGhqgMOI6q91KG4gzhIfDcEipcj6\nWPXIBiWa7LHJaHHJzzcQiduApwKBgQDDuQTJFnL6IQ97pM+YpVbNwpG+Lm11HKYw\ngHb0GsQDmXv/T8nbhOJPns0UYrELXXzaiBzr1s80nz66UGidYLxZSUlr65BN/A62\n9ledqk/IvjNNSuCDGq2L+m1hhbQRVWU4gz9tka+xFTeXJ2lKFxA/phLd4izbJUFp\neXjYuN76DQKBgGCN9ZLcIJEYvx2D1QyPGI1J3MZaYLAkS/NSGrrq5BtFM3ncuqsm\noUtIbVVRV+RPJBcueGKpv4EA5lIB7WbGBeutu+VONl5UKi56iYI+4v/+v+5+/8o8\ndHEQwI/m/psZ8qLLymzbIvHQ2/vBcs7fy7mbDn2admv5e9QgfkfjjL5ZAoGAbwk8\nCcjOmdC/s+mgTH0gbcMAY+B7AIGsVr/KvFmi8lfU3NcrMXqF2Z3BwtrqjgQPnPqz\nTaikDLp7H8AWWZJTyGOnX65YQ6XHw0ymEDBa6wvclvDvxfEQm+UKwNTVfy/vKMxs\n44BPKCtdSkd1mC7VbQaOlYqG4ByWq3vabzkSZtECgYBretAjd36vXhzUHV7A1T3H\nw5531/n3McUScDNMRrR96Bm32YgpSe4TOVDzlCOXh/cxd2GoxvNaVwWovuDYWKYA\nAV8GzHcR3D8Xo0unqg243dvynVMVTySs3G8lbWwzpoMhTGYfeZt9qHajquQSllgf\n6VpxznNOKtVnG3UigiLWSw==\n-----END PRIVATE KEY-----\n"",
                          ""client_email"": ""gemini-ai-ek-sa@plenary-song-384814.iam.gserviceaccount.com"",
                          ""client_id"": ""115492882060992421200"",
                          ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                          ""token_uri"": ""https://oauth2.googleapis.com/token"",
                          ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
                          ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/gemini-ai-ek-sa%40plenary-song-384814.iam.gserviceaccount.com"",
                          ""universe_domain"": ""googleapis.com""
                        }
                    ";
                    break;
                default:
                    jsonToken = "not implemented";
                    break;
            }

            GoogleCredential credential = GoogleCredential.FromJson(jsonToken);

            if (credential.IsCreateScopedRequired)
            {
                credential = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");
            }

            var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            return accessToken;
        }

        public static string GetProjectId()
        {
            string omgeving = "DEV";

            switch (omgeving)
            {
                case "DEV":
                    return "79429336350";
                default:
                    return null;
            }
        }
    }
}
