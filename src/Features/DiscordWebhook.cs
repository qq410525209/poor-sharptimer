/*
Copyright (C) 2024 Dea Brcka

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml;
using CounterStrikeSharp.API.Core;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private async Task GetDiscordWebhookURLFromConfigFile(string discordURLpath)
        {
            try
            {
                using JsonDocument? jsonConfig = await Utils.LoadJson(discordURLpath)!;
                if (jsonConfig != null)
                {
                    JsonElement root = jsonConfig.RootElement;
                
                    T GetPropertyValue<T>(string propertyName, T defaultValue, Func<JsonElement, T> getValue) {
                        if (root.TryGetProperty(propertyName, out var property)) {
                            try {
                                return getValue(property);
                            } catch (Exception ex) {
                                Utils.LogError($"Error parsing {propertyName}: {ex.Message}");
                                return defaultValue;
                            }
                        }
                        return defaultValue;
                    }
                    
                    discordWebhookBotName = GetPropertyValue("DiscordWebhookBotName", "SharpTimer", 
                        prop => prop.GetString() ?? "SharpTimer");
                    discordWebhookPFPUrl = GetPropertyValue("DiscordWebhookPFPUrl", 
                        "https://cdn.discordapp.com/icons/1196646791450472488/634963a8207fdb1b30bf909d31f05e57.webp",
                        prop => prop.GetString() ?? "");
                    discordWebhookImageRepoURL = GetPropertyValue("DiscordWebhookMapImageRepoUrl", 
                        "https://raw.githubusercontent.com/Letaryat/poor-sharptimermappics/main/pics/",
                        prop => prop.GetString() ?? "");
                    discordACWebhookUrl = GetPropertyValue("DiscordACWebhookUrl", "", prop => prop.GetString() ?? "");
                    discordPBWebhookUrl = GetPropertyValue("DiscordPBWebhookUrl", "", prop => prop.GetString() ?? "");
                    discordSRWebhookUrl = GetPropertyValue("DiscordSRWebhookUrl", "", prop => prop.GetString() ?? "");
                    discordPBBonusWebhookUrl = GetPropertyValue("DiscordPBBonusWebhookUrl", "", prop => prop.GetString() ?? "");
                    discordSRBonusWebhookUrl = GetPropertyValue("DiscordSRBonusWebhookUrl", "", prop => prop.GetString() ?? "");
                    discordWebhookFooter = GetPropertyValue("DiscordFooterString", "", prop => prop.GetString() ?? "");
                    discordWebhookRareGif = GetPropertyValue("DiscordRareGifUrl", "", prop => prop.GetString() ?? "");
                    
                    discordWebhookRareGifOdds = GetPropertyValue("DiscordRareGifOdds", 10000, 
                        prop => prop.GetInt32());
                    discordWebhookColor = GetPropertyValue("DiscordWebhookColor", 13369599, 
                        prop => prop.GetInt32());
                    
                    discordWebhookSteamAvatar = GetPropertyValue("DiscordWebhookSteamAvatar", true, 
                        prop => prop.GetBoolean());
                    discordWebhookTier = GetPropertyValue("DiscordWebhookTier", true, 
                        prop => prop.GetBoolean());
                    discordWebhookTimeChange = GetPropertyValue("DiscordWebhookTimeChange", true, 
                        prop => prop.GetBoolean());
                    discordWebhookTimesFinished = GetPropertyValue("DiscordWebhookTimesFinished", true, 
                        prop => prop.GetBoolean());
                    discordWebhookPlacement = GetPropertyValue("DiscordWebhookPlacement", true, 
                        prop => prop.GetBoolean());
                    discordWebhookSteamLink = GetPropertyValue("DiscordWebhookSteamLink", true, 
                        prop => prop.GetBoolean());
                    discordWebhookDisableStyleRecords = GetPropertyValue("DiscordWebhookDisableStyleRecords", true, 
                        prop => prop.GetBoolean());
                }
                else
                {
                    Utils.LogError($"DiscordWebhookUrl json was null");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error in GetDiscordWebhookURLFromConfigFile: {ex.Message}");
            }
        }

        public async Task DiscordRecordMessage(CCSPlayerController? player, string playerName, string runTime, string steamID, string placement, int timesFinished, bool isSR = false, string timeDifference = "", int bonusX = 0)
        {
            try
            {
                string? webhookURL = "your_discord_webhook_url";
                if (isSR && bonusX != 0)
                    webhookURL = discordSRBonusWebhookUrl;
                else if (isSR && bonusX == 0)
                    webhookURL = discordSRWebhookUrl;
                else if (!isSR && bonusX != 0)
                    webhookURL = discordPBBonusWebhookUrl;
                else if (!isSR && bonusX == 0)
                    webhookURL = discordPBWebhookUrl;

                if (string.IsNullOrEmpty(webhookURL) || webhookURL == "your_discord_webhook_url")
                {
                    Utils.LogError($"DiscordWebhookUrl was invalid");
                    return;
                }

                string mapImg = await GetMapImage(bonusX);
                bool isFirstTime = string.IsNullOrEmpty(timeDifference);
                string style = GetNamedStyle(playerTimers[player!.Slot].currentStyle);

                using var client = new HttpClient();

                var fields = new List<object>();

                if (!string.IsNullOrEmpty(currentMapName))
                {
                    fields.Add(new
                    {
                        name = "🗺️ Map:",
                        value = $"{(bonusX == 0 ? currentMapName : $"{currentMapName} bonus #{bonusX}")}",
                        inline = true
                    });
                }

                if (discordWebhookTier && currentMapTier != null)
                {
                    fields.Add(new
                    {
                        name = "🔰 Tier:",
                        value = currentMapTier,
                        inline = true
                    });
                }

                if (!string.IsNullOrEmpty(runTime))
                {
                    fields.Add(new
                    {
                        name = "⌛ Time:",
                        value = runTime,
                        inline = true
                    });
                }

                if (discordWebhookTimeChange && !isFirstTime)
                {
                    fields.Add(new
                    {
                        name = "⏳ Time change:",
                        value = timeDifference,
                        inline = true
                    });
                }

                if (discordWebhookPlacement && !string.IsNullOrEmpty(placement))
                {
                    fields.Add(new
                    {
                        name = "🎖️ Placement:",
                        value = $"#{placement}",
                        inline = true
                    });
                }

                if (discordWebhookTimesFinished)
                {
                    fields.Add(new
                    {
                        name = "🔢 Times Finished:",
                        value = $"{(!isFirstTime ? timesFinished : "First time!")}",
                        inline = true
                    });
                }

                if (discordWebhookSteamLink && !string.IsNullOrEmpty(steamID))
                {
                    fields.Add(new
                    {
                        name = "🛈 SteamID:",
                        value = $"[Profile](https://steamcommunity.com/profiles/{steamID})",
                        inline = true
                    });
                }

                if (!discordWebhookDisableStyleRecords && !string.IsNullOrEmpty(style))
                {
                    fields.Add(new
                    {
                        name = "🛹 Style:",
                        value = style,
                        inline = true
                    });
                }

                var spacedFields = new List<object>();
                for (int i = 0; i < fields.Count; i++)
                {
                    spacedFields.Add(fields[i]);
                    if ((i + 1) % 2 == 0 && i != fields.Count - 1)
                    {
                        spacedFields.Add(new
                        {
                            name = "\u200B",
                            value = "\u200B",
                            inline = true
                        });
                    }
                }
                if (fields.Count % 2 == 0)
                {
                    spacedFields.Add(new
                    {
                        name = "\u200B",
                        value = "\u200B",
                        inline = true
                    });
                }

                var embed = new Dictionary<string, object>
                {
                    { "title", !isSR ? $"set a new Personal Best!" : $"set a new Server Record!" },
                    { "fields", spacedFields.ToArray() },
                    { "author", new { name = $"{playerName}", url = $"https://steamcommunity.com/profiles/{steamID}" } },
                    { "footer", new { text = discordWebhookFooter, icon_url = discordWebhookPFPUrl } },
                    { "image", new { url = mapImg } }
                };

                if (discordWebhookColor != 0)
                    embed.Add("color", discordWebhookColor);

                if (discordWebhookSteamAvatar)
                    embed.Add("thumbnail", new { url = await GetAvatarLink($"https://steamcommunity.com/profiles/{steamID}/?xml=1") });

                var payload = new
                {
                    content = (string?)null,
                    embeds = new[] { embed },
                    username = discordWebhookBotName,
                    avatar_url = discordWebhookPFPUrl,
                    attachments = Array.Empty<object>()
                };

                var json = JsonSerializer.Serialize(payload);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                if (discordWebhookDisableStyleRecords && style != "Normal")
                    return;

                HttpResponseMessage response = await client.PostAsync(webhookURL, data);

                if (!response.IsSuccessStatusCode)
                {
                    Utils.LogError($"Failed to send message. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"An error occurred while sending Discord PB message: {ex.Message}");
            }
        }

        public async Task DiscordACMessage(CCSPlayerController? player, string reason)
        {
            try
            {
                string? webhookURL = discordACWebhookUrl;

                if (string.IsNullOrEmpty(webhookURL))
                {
                    Utils.LogError($"DiscordACWebhookUrl was invalid");
                    return;
                }
                using var client = new HttpClient();

                var fields = new List<object>();

                if (discordWebhookSteamLink && !string.IsNullOrEmpty(player!.SteamID.ToString()))
                {
                    fields.Add(new
                    {
                        name = "🛈 SteamID:",
                        value = $"[Profile](https://steamcommunity.com/profiles/{player!.SteamID})",
                        inline = true
                    });
                }
                fields.Add(new
                {
                    name = "Reason:",
                    value = $"{reason}",
                    inline = true
                });

                var spacedFields = new List<object>();
                for (int i = 0; i < fields.Count; i++)
                {
                    spacedFields.Add(fields[i]);
                    if ((i + 1) % 2 == 0 && i != fields.Count - 1)
                    {
                        spacedFields.Add(new
                        {
                            name = "\u200B",
                            value = "\u200B",
                            inline = true
                        });
                    }
                }
                if (fields.Count % 2 == 0)
                {
                    spacedFields.Add(new
                    {
                        name = "\u200B",
                        value = "\u200B",
                        inline = true
                    });
                }

                var embed = new Dictionary<string, object>
                {
                    { "title", "Player Flagged" },
                    { "fields", spacedFields.ToArray() },
                    { "author", new { name = $"{player!.PlayerName}", url = $"https://steamcommunity.com/profiles/{player.SteamID}" } },
                    { "footer", new { text = discordWebhookFooter, icon_url = discordWebhookPFPUrl } }
                };

                if (discordWebhookColor != 0)
                    embed.Add("color", discordWebhookColor);

                if (discordWebhookSteamAvatar)
                    embed.Add("thumbnail", new { url = await GetAvatarLink($"https://steamcommunity.com/profiles/{player.SteamID}/?xml=1") });

                var payload = new
                {
                    content = (string?)null,
                    embeds = new[] { embed },
                    username = discordWebhookBotName,
                    avatar_url = discordWebhookPFPUrl,
                    attachments = Array.Empty<object>()
                };

                var json = JsonSerializer.Serialize(payload);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(webhookURL, data);

                if (!response.IsSuccessStatusCode)
                {
                    Utils.LogError($"Failed to send message. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"An error occurred while sending Discord AC message: {ex.Message}");
            }
        }

        public async Task<string> GetMapImage(int bonusX = 0)
        {
            if (new Random().Next(1, discordWebhookRareGifOdds + 1) == 69)
            {
                if (string.IsNullOrEmpty(discordWebhookRareGif))
                    return "https://files.catbox.moe/q99x7v.gif";
                else
                    return discordWebhookRareGif;
            }

            string imageRepo = $"{discordWebhookImageRepoURL}{(bonusX == 0 ? currentMapName : $"{currentMapName}_b{bonusX}")}.jpg";
            string error = $"{discordWebhookImageRepoURL}{(currentMapName!.Contains("surf_") ? "surf404" : $"{(currentMapName!.Contains("bhop_") ? "bhop404" : "404")}")}.jpg";
            try
            {
                using var client = new HttpClient();
                if (!await Is404(client, imageRepo))
                {
                    return imageRepo;
                }
                else
                {
                    return error;
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"Failed to get DiscordWebhook img. {ex.Message}");
                return error;
            }
        }

        static async Task<bool> Is404(HttpClient client, string url)
        {
            try
            {
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

                return response.StatusCode == HttpStatusCode.NotFound;
            }
            catch (HttpRequestException)
            {
                return true;
            }
        }

        public async Task<string> GetAvatarLink(string xmlUrl)
        {
            try
            {
                using var client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(xmlUrl);
                response.EnsureSuccessStatusCode();
                string xmlContent = await response.Content.ReadAsStringAsync();

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlContent);

                XmlNode? avatarFullNode = xmlDoc.SelectSingleNode("//avatarFull");

                string avatarFullLink = avatarFullNode!.InnerText.Trim();

                return avatarFullLink;
            }
            catch (Exception ex)
            {
                Utils.LogError("GetAvatarLink Error occurred: " + ex.Message);
                return "https://cdn.discordapp.com/icons/1196646791450472488/634963a8207fdb1b30bf909d31f05e57.webp";
            }
        }
    }
}