export type WalletTransactionType = "Recharge" | "ConsumptionDebit" | "Adjustment";

export interface PrepaidAccount {
  id: string;
  customerId: string;
  balance: number;
  isConnected: boolean;
}

export interface WalletTransaction {
  id: string;
  prepaidAccountId: string;
  type: WalletTransactionType;
  amount: number;
  balanceAfter: number;
  reference: string;
  note: string | null;
  createdAtUtc: string;
}
