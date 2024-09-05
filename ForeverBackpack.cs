#region License (GPL v2)
/*
    ForeverBackpack
    Copyright (c) 2024 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License v2.0.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion
using Oxide.Core;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("ForeverBackpack", "RFC1920", "0.0.2")]
    [Description("Restore contents of worn Rust backpack at wipe")]
    internal class ForeverBackpack : RustPlugin
    {
        private ConfigData configData;
        private bool newsave;
        public static ForeverBackpack Instance;

        private Dictionary<ulong, List<BPItem>> _backpacks = new Dictionary<ulong, List<BPItem>>();
        private List<ulong> reloaded = new List<ulong>();

        public class BPItem
        {
            public int ID;
            public string Name;
            public int Position;
            public int Amount;
            public int AmmoAmount;
            public float Condition;
            public float MaxCondition;
        }

        private void DoLog(string message)
        {
            if (configData.Options.debug) Interface.GetMod().LogInfo($"[{Name}] {message}");
        }

        private void OnServerInitialized()
        {
            LoadConfigValues();
            LoadData();
            if (newsave)
            {
                reloaded.Clear();
                newsave = false;
            }
            Instance = this;
        }

        private void OnNewSave()
        {
            newsave = true;
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.allPlayerList)
            {
                if (player == null) continue;
                if (!player.userID.IsSteamId()) continue;

                PlayerDisconnect(player);
            }
            SaveData();
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            PlayerDisconnect(player);
        }

        private void PlayerDisconnect(BasePlayer player)
        {
            if (player == null) return;
            if (!player.userID.IsSteamId()) return;

            DoLog($"Got player {player?.displayName}");
            if (!_backpacks.ContainsKey(player.userID))
            {
                _backpacks.Add(player.userID, new List<BPItem>());
            }
            DoLog("Checking for backpack");
            Item backpack = player.inventory.containerWear.FindItemsByItemID(-907422733).FirstOrDefault();
            if (backpack != null)
            {
                DoLog("Found worn backpack!");
                _backpacks[player.userID].Clear();
                ItemContainer cont = backpack.contents;
                for (int i = 0; i < cont.capacity; i++)
                {
                    Item item = cont.GetSlot(i);
                    if (item != null)
                    {
                        DoLog($"Found item {item.info.displayName.english} in backpack, stacked at {item.amount} in slot {item.position}");
                        BPItem bPItem = new BPItem();
                        if (item.info != null)
                        {
                            bPItem.Amount = item.amount > 0 ? item.amount : 1;
                            bPItem.Name = item.info.displayName.english;
                            bPItem.Condition = item.condition;
                            bPItem.MaxCondition = item.maxCondition;
                            bPItem.ID = item.info.itemid;
                            bPItem.Position = item.position;
                            bPItem.AmmoAmount = item.ammoCount != null ? item.ammoCount.Value : 0;
                        }
                        DoLog("Adding to saved backpack inventory");
                        _backpacks[player.userID].Add(bPItem);
                        reloaded.Add(player.userID);
                    }
                }
            }
            SaveData();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            // We do this here vs @ wipe since the players are not yet present then.
            if (!player.userID.IsSteamId()) return;
            // This should ensure this only happens once per wipe on first connect.
            if (reloaded.Contains(player.userID)) return;

            // Add backpack and contents if available
            if (!_backpacks.ContainsKey(player.userID) && configData.Options.AlwaysIssueBackpack)
            {
                // New player, and we have decided to issue a backpack
                if (player.inventory.containerWear != null && player.inventory.containerWear.IsEmpty())
                {
                    // Add empty BP to inventory
                    DoLog($"Adding empty backpack for {player?.displayName}");
                    Item item = ItemManager.CreateByItemID(configData.Options.UseLargeBackpack ? -907422733 : 2068884361);
                    item.MoveToContainer(player.inventory.containerWear);
                    reloaded.Add(player.userID);
                }
            }
            else if (_backpacks.ContainsKey(player.userID))
            {
                // Normal reload process
                if (player.inventory.containerWear != null && player.inventory.containerWear.IsEmpty())
                {
                    // Add BP to inventory
                    DoLog($"Restoring backpack for {player?.displayName}");
                    Item item = ItemManager.CreateByItemID(configData.Options.UseLargeBackpack ? -907422733 : 2068884361);
                    int capacity = item.contents.capacity;
                    item.MoveToContainer(player.inventory.containerWear);
                    // Add items to BP
                    foreach (BPItem b in _backpacks[player.userID])
                    {
                        Item bpitem = ItemManager.CreateByItemID(b.ID);
                        bpitem.amount = b.Amount;
                        bpitem.condition = b.Condition;
                        bpitem.maxCondition = b.MaxCondition;
                        bpitem.condition = b.Condition;
                        bpitem.ammoCount = b.AmmoAmount;
                        if (b.Position > capacity)
                        {
                            // Hopefully satisfies the case where the admin switched to small backpacks at wipe
                            try
                            {
                                bpitem.MoveToContainer(player.inventory.containerWear);
                            }
                            catch { }
                            continue;
                        }
                        // Normal operation - item will be restored to its original position
                        bpitem.MoveToContainer(item.contents, b.Position);
                    }
                    player.inventory.containerWear.MarkDirty();
                }
                reloaded.Add(player.userID);
            }
        }

        #region Data
        private void LoadData()
        {
            _backpacks = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, List<BPItem>>>(Name + "/backpacks");
            reloaded = Interface.GetMod().DataFileSystem.ReadObject<List<ulong>>(Name + "/reloaded");
        }

        private void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject(Name + "/backpacks", _backpacks);
            Interface.GetMod().DataFileSystem.WriteObject(Name + "/reloaded", reloaded);
        }
        #endregion Data

        #region config
        private class ConfigData
        {
            public Options Options;
            public VersionNumber Version;
        }

        private class Options
        {
            public bool UseLargeBackpack;
            public bool AlwaysIssueBackpack;
            public bool debug;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                Options = new Options()
                {
                    UseLargeBackpack = true,
                    AlwaysIssueBackpack = false,
                    debug = false
                },
                Version = Version
            };
            SaveConfig(config);
        }

        private void LoadConfigValues()
        {
            configData = Config.ReadObject<ConfigData>();
            configData.Version = Version;

            SaveConfig(configData);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
        #endregion
    }
}
