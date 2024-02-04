using Life;
using Life.BizSystem;
using Life.UI;
using MyMenu.Entities;
using System;
using UnityEngine;
using Life.Network;
using Life.VehicleSystem;
using UIPanelManager;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Config.config;
using Newtonsoft.Json;
using Life.DB;

namespace FrenchFourriere
{
    public class frenchFourriere : Plugin
    {
        Dictionary<uint, long> Temps = new Dictionary<uint, long>();
        Dictionary<uint, long> Temps3 = new Dictionary<uint, long>();
        private int _temps;
        private string _webhook;

        public frenchFourriere(IGameAPI api) : base(api)
        {
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            Debug.Log("FrenchFourriere a été initialisé avec succès");

            Section section = new Section("FrenchFourriere", "FrenchFourriere", "v1.0.0", "French Aero");
            Action<UIPanel> action = ui => MaFonction(section.GetPlayer(ui));
            section.SetBizIdAllowed();
            section.SetBizTypeAllowed(Activity.Type.Fourriere);
            section.OnlyAdmin = false;
            section.MinAdminLevel = 0;
            section.Line = new UITabLine(section.Title, action);
            section.Insert(false);

            var configFilePath = Path.Combine(pluginsPath, "FrenchFourriere/config.json");
            var globalConfiguration = ChargerConfiguration(configFilePath);
            _temps = globalConfiguration.temps;
            _webhook = globalConfiguration.Webhook;
        }

        public void MaFonction(Player player)
        {
            UIPanel panel = new UIPanel("Fourriere", UIPanel.PanelType.Tab);
            panel.SetText("Fourriere");
            panel.AddTabLine("Démarrer le véhicule", (Action<UIPanel>)(ui =>
            {
                Vehicle vehicle = player.GetClosestVehicle(10);
                if (vehicle != null)
                {
                    DateTime Temps2 = DateTime.Now;
                    if (Temps.ContainsKey(player.netId))
                    {
                        if (Temps2.Ticks - Temps[player.netId] > TimeSpan.FromMinutes(_temps).Ticks)
                        {
                            Temps[player.netId] = Temps2.Ticks;
                            vehicle.CanEngineStart = true;
                            player.ClosePanel(panel);
                            SendWebhook(_webhook, player.GetFullName() + " vien de démarrer un véhicule portant la plaque d'immatriculation: " + vehicle.plate);
                        }
                        else
                            PanelManager.Notification(player, "Fourriere", "Vous pouvez démarrer un véhicule uniquement toute les " + _temps + " minutes", NotificationManager.Type.Warning);
                    }
                    else
                    {
                        Temps.Add(player.netId, Temps2.Ticks);
                        vehicle.CanEngineStart = true;
                        player.ClosePanel(panel);
                        SendWebhook(_webhook, player.GetFullName() + " vien de démarrer un véhicule portant la plaque d'immatriculation: " + vehicle.plate);
                    }
                }
                else
                    PanelManager.Notification(player, "Fourriere", "Vous n'êtes pas proche d'un véhicule!", NotificationManager.Type.Warning);
            }));
            panel.AddTabLine("Eteignez le véhicule", (Action<UIPanel>)(ui =>
            {
                Vehicle vehicle = player.GetClosestVehicle(10);
                if (vehicle != null)
                {
                    DateTime Temps2 = DateTime.Now;
                    if (Temps3.ContainsKey(player.netId))
                    {
                        if (Temps2.Ticks - Temps3[player.netId] > TimeSpan.FromMinutes(_temps).Ticks)
                        {
                            Temps3[player.netId] = Temps2.Ticks;
                            vehicle.CanEngineStart = false;
                            player.ClosePanel(panel);
                            SendWebhook(_webhook, player.GetFullName() + " vien d'éteindre un véhicule portant la plaque d'immatriculation: " + vehicle.plate);
                        }
                        else
                            PanelManager.Notification(player, "Fourriere", "Vous pouvez démarrer un véhicule uniquement toute les " + _temps + " minutes", NotificationManager.Type.Warning);
                    }
                    else
                    {
                        Temps3.Add(player.netId, Temps2.Ticks);
                        vehicle.CanEngineStart = false;
                        player.ClosePanel(panel);
                        SendWebhook(_webhook, player.GetFullName() + " vien d'éteindre un véhicule portant la plaque d'immatriculation: " + vehicle.plate);
                    }
                }
                else
                    PanelManager.Notification(player, "Fourriere", "Vous n'êtes pas proche d'un véhicule!", NotificationManager.Type.Warning);
            }));
            panel.AddButton("Fermer", (Action<UIPanel>)(ui =>
            {
                player.ClosePanel(panel);
            }));
            panel.AddButton("Valider", (Action<UIPanel>)(ui =>
            {
                panel.SelectTab();
            }));
            if (player.serviceMetier == true)
            {
                player.ShowPanelUI(panel);
            }
            else
                PanelManager.Notification(player, "Fourriere", "Vous devez être en service métier pour faire cette action!", NotificationManager.Type.Warning);
        }

        private static MainConfig ChargerConfiguration(string configFilePath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(configFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
            }
            if (!File.Exists(configFilePath))
            {
                File.WriteAllText(configFilePath, "{\n  \"temps\": 3,\n  \"webhook\": null\n}");
            }
            var jsonConfig = File.ReadAllText(configFilePath);
            return JsonConvert.DeserializeObject<MainConfig>(jsonConfig);
        }

        private static async Task SendWebhook(string webhookUrl, string content)
        {
            using (var client = new HttpClient())
            {
                var payload = new
                {
                    content = content
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(webhookUrl, data);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Erreur lors de l'envoi du webhook. Statut : {response.StatusCode}");
                }
            }
        }
    }
}
