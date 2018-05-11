using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Neo.SmartContract
{
    public class SmartContractTemplate : Framework.SmartContract
    {
        //Token Settings
        public static string Name() => "Energy";
        public static string Symbol() => "NRG";
		public static readonly byte[] Owner = "CHANGE_TO_CONTRACT_OWNER_HASH".ToScriptHash();
		public static byte Decimals() => 0;
        private const ulong total_amount = 8000000; // total token amount

		public delegate void MyAction<T, T1, T2>(T p0, T1 p1, T2 p2);

		[DisplayName("transfer")]
		public static event MyAction<byte[], byte[], BigInteger> Transferred;

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                if (Owner.Length == 20)
                {
                    // if param Owner is script hash
                    return Runtime.CheckWitness(Owner);
                }
                else if (Owner.Length == 33)
                {
                    // if param Owner is public key
                    byte[] signature = operation.AsByteArray();
                    return VerifySignature(signature, Owner);
                }
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "deploy") return Deploy();
				if (operation == "retributeTokens") return RetributeTokens((byte[])args[0], (String)args[1], (BigInteger)args[2]);
				if (operation == "consumeTokens") return ConsumeTokens((byte[])args[0], (BigInteger)args[1]);
				if (operation == "totalSupply") return TotalSupply();
                if (operation == "name") return Name();
                if (operation == "symbol") return Symbol();
                if (operation == "transfer")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Transfer(from, to, value);
                }
                if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[])args[0];
                    return BalanceOf(account);
                }
                if (operation == "decimals") return Decimals();
            }
            return false;
        }

        // initialization parameters, only once
        public static bool Deploy()
        {
            byte[] total_supply = Storage.Get(Storage.CurrentContext, "totalSupply");
            if (total_supply.Length != 0) return false;
			Storage.Put(Storage.CurrentContext, Owner, total_amount);
			Storage.Put(Storage.CurrentContext, "totalSupply", total_amount);
			Transferred(null, Owner, total_amount);
			return true;
        }

        // Retribute tokens to invoker
		public static bool RetributeTokens(byte[] to, String hash, BigInteger kwh)
		{
			byte[] from = Owner;
     
			if (!Runtime.CheckWitness(to)) return false;
			if (kwh <= 0) return false;
            if (from == to) return true;
            
			BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < kwh) return false;
			if (from_value == kwh)
                Storage.Delete(Storage.CurrentContext, from);
            else
				Storage.Put(Storage.CurrentContext, from, from_value - kwh);

			Runtime.Notify("SAVING TELEMETRY HASH REPORT");       
			Storage.Put(Storage.CurrentContext, from, hash);
            
            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
			Storage.Put(Storage.CurrentContext, to, to_value + kwh);
			Transferred(from, to, kwh);
			Runtime.Notify("RETRIBUTE TOKENS TO USER");
			return true;
		}

		public static bool ConsumeTokens(byte[] from, BigInteger value)
		{
            byte[] to = Owner;

			if (!Runtime.CheckWitness(from)) return false;
            if (value <= 0) return false;
            if (from == to) return true;

            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;
            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, from);
            else
                Storage.Put(Storage.CurrentContext, from, from_value - value);
            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, to, to_value + value);
            Transferred(from, to, value);
            Runtime.Notify("CONSUME", to);
            return true;
		}
          
        // get the total token supply
        public static BigInteger TotalSupply()
        {
            return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
        }

        // function that is always called when someone wants to transfer tokens.
        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            if (from == to) return true;
            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;
            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, from);
            else
                Storage.Put(Storage.CurrentContext, from, from_value - value);
            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, to, to_value + value);
            Transferred(from, to, value);
            return true;
        }
        
		// get the account balance of another account with address
		public static BigInteger BalanceOf(byte[] address)
		{
			return Storage.Get(Storage.CurrentContext, address).AsBigInteger();
		}
    }
}