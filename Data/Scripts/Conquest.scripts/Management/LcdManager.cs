﻿namespace Conquest.scripts.Management
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Conquest.scripts;
    using Conquest.scripts.ConqStructures;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    public class LcdManager
    {
        public static void UpdateLcds()
        {
			
            if (!ConquestScript.Instance.Config.EnableLcds)
                return;

            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null);
            var updatelist = new HashSet<IMyTextPanel>();

            foreach (var player in players)
            {
                // Establish a visual range of the LCD.
                // if there are no players closer than this, don't bother updating it.
                var sphere = new BoundingSphereD(player.GetPosition(), 75);
                var list = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
                foreach (var block in list)
                {
                    // TODO: projected ship check?
                    var textPanel = block as IMyTextPanel;
                    if (textPanel != null
                        && textPanel.IsFunctional
                        && textPanel.IsWorking
                        && ConquestConsts.LCDTags.Any(tag => textPanel.CustomName.IndexOf(tag, StringComparison.InvariantCultureIgnoreCase) >= 0))
                    {
                        updatelist.Add((IMyTextPanel)block);
                    }
                }
            }

            foreach (var textPanel in updatelist)
                ProcessLcdBlock(textPanel);
        }

        public static void BlankLcds()
        {
            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, e => e is IMyCubeGrid);

            foreach (var entity in entities)
            {
                var cubeGrid = (IMyCubeGrid) entity;
                if (cubeGrid.Physics == null)
                    continue;

                var blocks = new List<IMySlimBlock>();
                cubeGrid.GetBlocks(blocks, block => block != null && block.FatBlock != null &&
                    block.FatBlock.BlockDefinition.TypeId == typeof (MyObjectBuilder_TextPanel) &&
                    ConquestConsts.LCDTags.Any(tag => ((IMyTerminalBlock) block.FatBlock).CustomName.IndexOf(tag, StringComparison.InvariantCultureIgnoreCase) >= 0));

                foreach (var block in blocks)
                {
                    var writer = TextPanelWriter.Create((IMyTextPanel)block.FatBlock);
                    writer.AddPublicLine("Conquest LCD is disabled");
                    writer.UpdatePublic();
                }
            }
        }

        private static void ProcessLcdBlock(IMyTextPanel textPanel)
        {
            //counter++;

            var checkArray = (textPanel.GetPublicTitle() + " " + textPanel.GetPrivateTitle()).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            bool showLeaderboard = false;
            //bool showConfig = false;


            // removed Linq, to reduce the looping through the array. This should only have to do one loop through all items in the array.
            foreach (var str in checkArray)
            {
				if (str.Equals("Leaderboard", StringComparison.InvariantCultureIgnoreCase))
					showLeaderboard = true;
				//else if (str.StartsWith("Config", StringComparison.InvariantCultureIgnoreCase))
				//	showConfig = true;
            }

            bool showHelp = !showLeaderboard;

            var writer = TextPanelWriter.Create(textPanel);

            // Use the update interval on the LCD Panel to determine how often the display is updated.
            // It can only go as fast as the timer calling this code is.
            var interval = Math.Max(1f, textPanel.GetValueFloat("ChangeIntervalSlider"));
            if (writer.LastUpdate > DateTime.Now.AddSeconds(-interval))
                return;

            if (showHelp)
            {
                writer.AddPublicLine("Please add a tag to the private or public title.");
                writer.AddPublicLine("ie., * Leaderboard");
                writer.UpdatePublic();
                return;
            }
			if (showLeaderboard)
			{
				writer.AddPublicCenterLine(TextPanelWriter.LcdLineWidth / 2f, "Conquest Faction Leaderboard");

				
				if (writer.IsWide)
				{
					var NameColumn = TextPanelWriter.LcdLineWidth - 550;
					var VictoryPointsColumn = TextPanelWriter.LcdLineWidth - 240;
					var PlanetBasesColumn = TextPanelWriter.LcdLineWidth - 160;
					var MoonBasesColumn = TextPanelWriter.LcdLineWidth - 80;
					var AsteroidBasesColumn = TextPanelWriter.LcdLineWidth;
					writer.AddPublicText("Tag");
					writer.AddPublicText("  Name");
					writer.AddPublicRightText(VictoryPointsColumn, "Points");
					if (ConquestScript.Instance.Config.PlanetPoints > 0)
					writer.AddPublicRightText(PlanetBasesColumn, "Planet");
					if (ConquestScript.Instance.Config.MoonPoints > 0)
					writer.AddPublicRightText(MoonBasesColumn, "Moon");
					if (ConquestScript.Instance.Config.AsteroidPoints > 0)
					writer.AddPublicRightText(AsteroidBasesColumn, "Asteroid");
					writer.AddPublicLine();
					foreach (ConquestFaction Faction in ConquestScript.Instance.Data.ConquestFactions)
					{
						if (Faction.VictoryPoints > 0)
						{
							writer.AddPublicLeftTrim(NameColumn - 50, Faction.FactionTag);
                            if (Faction.FactionName.Length > 40)
                            {
                                writer.AddPublicText("    " + Faction.FactionName.Substring(0, 40) + "...");
                            }
                            else
                            {
                                writer.AddPublicText("    " + Faction.FactionName);
                            }
							writer.AddPublicRightText(VictoryPointsColumn, Faction.VictoryPoints.ToString());
							if (ConquestScript.Instance.Config.PlanetPoints > 0)
							writer.AddPublicRightText(PlanetBasesColumn, Faction.PlanetBases.ToString());
							if (ConquestScript.Instance.Config.MoonPoints > 0)
							writer.AddPublicRightText(MoonBasesColumn, Faction.MoonBases.ToString());
							if (ConquestScript.Instance.Config.AsteroidPoints > 0)
							writer.AddPublicRightText(AsteroidBasesColumn, Faction.AsteroidBases.ToString());								

							writer.AddPublicLine();
						}

					}
				}
				else
				{
					var NameColumn = TextPanelWriter.LcdLineWidth - 300;
					var VictoryPointsColumn = TextPanelWriter.LcdLineWidth;
					writer.AddPublicText("Tag");
					writer.AddPublicText("  Name");
					writer.AddPublicRightLine(VictoryPointsColumn, "Points");
					foreach (ConquestFaction Faction in ConquestScript.Instance.Data.ConquestFactions)
					{
						if (Faction.VictoryPoints > 0)
						{
							writer.AddPublicLeftTrim(NameColumn - 25, Faction.FactionTag);
                            if (Faction.FactionName.Length > 40)
                            {
                                writer.AddPublicText("    " + Faction.FactionName.Substring(0, 30) + "...");
                            }
                            else
                            {
                                writer.AddPublicText("    " + Faction.FactionName);
                            }
                            writer.AddPublicRightText(VictoryPointsColumn, Faction.VictoryPoints.ToString());
							writer.AddPublicLine();
						}

					}
				}


				
			}	
            writer.UpdatePublic();
        }
		
		private static string FromBool(bool value)
		{
			if (value)
			{
				return "On";
			}
			else
			{
				return "Off";
			}
		}
    }
}
