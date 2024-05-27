using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Thirdweb;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class BlockchainManager : MonoBehaviour
{
    public UnityEvent<string> OnLoggedIn;

    public string Address { get; private set; }

    public static BlockchainManager Instance { get; private set; }

    public Button claimTokenButton;
    public TextMeshProUGUI claimTokenButtonText;
    public CharacterManager characterManagerRef;
    private string _distanceTravelled;

    public TextMeshProUGUI tokenBalanceAmountText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        _distanceTravelled = characterManagerRef.DistanceTravelled.ToString("F0");
    }

    public async void Login(string authProvider)
    {
        AuthProvider provider = AuthProvider.Google;
        switch (authProvider)
        {
            case "google":
                provider = AuthProvider.Google;
                break;
            case "apple":
                provider = AuthProvider.Apple;
                break;
            case "facebook":
                provider = AuthProvider.Facebook;
                break;
        }

        var connection = new WalletConnection(
            provider: WalletProvider.SmartWallet,
            chainId: 421614,
            personalWallet: WalletProvider.EmbeddedWallet,
            authOptions: new AuthOptions(authProvider: provider)
        );

        Address = await ThirdwebManager.Instance.SDK.wallet.Connect(connection);
        InvokeOnLoggedIn();
    }

    void InvokeOnLoggedIn() {
        OnLoggedIn?.Invoke(Address);
        GetTokenBalance();
    }

    internal async Task SubmitScore(float distanceTravelled)
    {
        Debug.Log($"Submitting score of {distanceTravelled} to blockchain for address {Address}");
        var contract = ThirdwebManager.Instance.SDK.GetContract(
            "0xf24CDD7513E2A7697Eb5f7e1Af7Acea52b015F46",
            "[{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"player\",\"type\":\"address\"},{\"indexed\":false,\"internalType\":\"uint256\",\"name\":\"score\",\"type\":\"uint256\"}],\"name\":\"ScoreAdded\",\"type\":\"event\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"player\",\"type\":\"address\"}],\"name\":\"getRank\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"rank\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"score\",\"type\":\"uint256\"}],\"name\":\"submitScore\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]"
        );
        await contract.Write("submitScore", (int)distanceTravelled);
    }

    internal async Task<int> GetRank()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(
            "0xf24CDD7513E2A7697Eb5f7e1Af7Acea52b015F46",
            "[{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"player\",\"type\":\"address\"},{\"indexed\":false,\"internalType\":\"uint256\",\"name\":\"score\",\"type\":\"uint256\"}],\"name\":\"ScoreAdded\",\"type\":\"event\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"player\",\"type\":\"address\"}],\"name\":\"getRank\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"rank\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"score\",\"type\":\"uint256\"}],\"name\":\"submitScore\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]"
        );
        var rank = await contract.Read<int>("getRank", Address);
        Debug.Log($"Rank for address {Address} is {rank}");
        return rank;
    }

    public async void ClaimToken() {
        claimTokenButtonText.text = "Claiming...";
        claimTokenButton.interactable = false;

        var contract = ThirdwebManager.Instance.SDK.GetContract("0xAD1E8389FA2B6885937c3B4De702249DBA6a0C54");
        var result = await contract.ERC20.ClaimTo(Address, _distanceTravelled);
        claimTokenButtonText.text = "Claimed Token!";
        GetTokenBalance();
    }

    public async void GetTokenBalance() {
        var contract = ThirdwebManager.Instance.SDK.GetContract("0xAD1E8389FA2B6885937c3B4De702249DBA6a0C54");
        var balance = await contract.ERC20.BalanceOf(Address);
        tokenBalanceAmountText.text = balance.displayValue;

    }
}
