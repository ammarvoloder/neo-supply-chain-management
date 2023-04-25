using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;

namespace SupplyChain
{
    public class SupplyChainContract : SmartContract
    {   
        public uint upc;
        public static uint sku = 0;

        [InitialValue("NRQmyij1kMEgWAnCzE8VP78j8HsbJrwng5", Neo.SmartContract.ContractParameterType.Hash160)]
        static readonly UInt160 Owner = default;
        private static bool IsOwner() => Runtime.CheckWitness(Owner);

        public enum State {
            Harvested, 
            Processed, 
            Packed, 
            ForSale,
            Sold, 
            Shipped,
            Received,
            Purchased
        } 

        private static State defaultState = State.Harvested;

        public struct Item {
            public uint sku;
            public uint upc;
            public ByteString ownerID; // address of the current owner 
            public ByteString farmerID; // address of the farmer
            public string farmName; 
            public string farmInformation; 
            public string farmLatitude;
            public string farmLongitude;
            public uint productID;
            public string productNotes;
            public uint productPrice;
            public State itemState;
            public ByteString distributorID;
            public ByteString retailerID;
            public ByteString consumerID;
            
        }
        private static StorageMap ContractStorage => new StorageMap(Storage.CurrentContext, "SupplyChainContract");
        private static StorageMap Roles => new StorageMap(Storage.CurrentContext, "Roles");

        private static StorageMap Items => new StorageMap(Storage.CurrentContext, "Items");

        private static Transaction Tx => (Transaction) Runtime.ScriptContainer;

        public static bool RegisterRole(string role, ByteString address) {
            if (IsOwner()) {
                Roles.Put(role, address);
                return true;
            }
            return false;
        }
        
         public static bool isFarmer(ByteString address) {
            ByteString farmerAddress = Roles.Get("Farmer");
            if (farmerAddress != null && address.Equals(farmerAddress)) {
                return true;
            }
            return false;
        }

        public static bool isRetailer(ByteString address) {
            ByteString retailerAddress = Roles.Get("Retailer");
            if (retailerAddress != null && address.Equals(retailerAddress)) {
                return true;
            }
            return false;
        }

         public static bool isConsumer(ByteString address) {
            ByteString consumerAddress = Roles.Get("Consumer");
            if (consumerAddress != null && address.Equals(consumerAddress)) {
                return true;
            }
            return false;
        }

         public static bool isDistributor(ByteString address) {
            ByteString distributorAddress = Roles.Get("Distributor");
            if (distributorAddress != null && address.Equals(distributorAddress)) {
                return true;
            }
            return false;
        }

        public static void HarvestItem(uint upc, ByteString farmer, string farmName, string farmInformation, string farmLatitude, string farmLongitude, string productNotes) {
            Item item = new Item();
            item.upc = upc; 
            item.sku = sku; 
            item.ownerID = farmer;
            item.farmerID = farmer;
            item.farmName = farmName;
            item.farmInformation = farmInformation;
            item.farmLongitude = farmLongitude;
            item.farmLatitude = farmLatitude;
            item.productNotes = productNotes;
            item.productPrice = 0;
            item.itemState = defaultState;
            item.distributorID = string.Empty;
            item.retailerID = string.Empty;
            item.consumerID = string.Empty;

            Items.PutObject(upc.ToString(), item);
            sku = sku + 1;

        }

        public static UInt160 GetSender() {
            var tx = (Transaction) Runtime.ScriptContainer;
            return (UInt160) tx.Sender;
        }
        
        public static bool IsHarvested(uint upc) {
            Item item = (Item) Items.GetObject(upc.ToString());
            if (item.itemState == State.Harvested) {
                return true;
            }
            return false;
        }

         public static bool IsProcessed(uint upc) {
            Item item = (Item) Items.GetObject(upc.ToString());
            if (item.itemState == State.Processed) {
                return true;
            }
            return false;
        }

         public static bool IsPacked(uint upc) {
            Item item = (Item) Items.GetObject(upc.ToString());
            if (item.itemState == State.Packed) {
                return true;
            }
            return false;
        }

         public static bool IsForSale(uint upc) {
            Item item = (Item) Items.GetObject(upc.ToString());
            if (item.itemState == State.ForSale) {
                return true;
            }
            return false;
        }

         public static bool IsSold(uint upc) {
            Item item = (Item) Items.GetObject(upc.ToString());
            if (item.itemState == State.Sold) {
                return true;
            }
            return false;
        }

         public static bool IsShipped(uint upc) {
            Item item = (Item) Items.GetObject(upc.ToString());
            if (item.itemState == State.Shipped) {
                return true;
            }
            return false;
        }

         public static bool IsPurchased(uint upc) {
            Item item = (Item) Items.GetObject(upc.ToString());
            if (item.itemState == State.Purchased) {
                return true;
            }
            return false;
        }
         public static bool IsReceived(uint upc) {
            Item item = (Item) Items.GetObject(upc.ToString());
            if (item.itemState == State.Received) {
                return true;
            } return false;
        }

        public static void ProcessItem(uint upc) {
            if (isFarmer(GetSender().ToString()) && IsHarvested(upc)) {
               Item item = (Item) Items.GetObject(upc.ToString());
               item.itemState = State.Processed;
               Items.PutObject(upc.ToString(), item);
            }
        }

        public static void PackItem(uint upc) {
            if (isFarmer(GetSender().ToString()) && IsProcessed(upc)) {
               Item item = (Item) Items.GetObject(upc.ToString());
               item.itemState = State.Packed;
               Items.PutObject(upc.ToString(), item);
            }
        }

        public static void SellItem(uint upc, uint price) {
            if (isFarmer(GetSender().ToString()) && IsPacked(upc)) {
                Item item = (Item) Items.GetObject(upc.ToString()); 
                item.productPrice = price;
                item.itemState = State.ForSale;
                Items.PutObject(upc.ToString(), item);
            }
        }

        public static void BuyItem(uint upc, uint price) {
            if (isDistributor(GetSender().ToString()) && IsForSale(upc)) {
                Item item = (Item) Items.GetObject(upc.ToString());
                if (item.productPrice <= price) {
                    item.distributorID = GetSender().ToString();
                    item.ownerID = GetSender().ToString();
                    item.itemState = State.Sold;
                    Items.PutObject(upc.ToString(), item);
                }
            }
        }

        public static void ShipItem(uint upc) {
            if (isDistributor(GetSender().ToString()) && IsSold(upc)) {
                Item item = (Item) Items.GetObject(upc.ToString());
                item.itemState = State.Shipped;
                Items.PutObject(upc.ToString(), item);

            }
        }

        public static void ReceiveItem(uint upc) {
            if (isRetailer(GetSender().ToString()) && IsShipped(upc)) {
                Item item = (Item) Items.GetObject(upc.ToString());
                item.ownerID = GetSender().ToString();
                item.retailerID = GetSender().ToString();
                item.itemState = State.Received;
                Items.PutObject(upc.ToString(), item);

            }
        }

        public static void PurchaseItem(uint upc) {
            if (isConsumer(GetSender().ToString()) && IsReceived(upc)) {
                Item item = (Item) Items.GetObject(upc.ToString());
                item.ownerID = GetSender().ToString();
                item.consumerID = GetSender().ToString();
                item.itemState = State.Purchased;
                Items.PutObject(upc.ToString(), item);
            }
        }

        public static Item GetItem(uint upc) {
            Item item = (Item) Items.GetObject(upc.ToString());
            return item;
        }

    
    }
}
