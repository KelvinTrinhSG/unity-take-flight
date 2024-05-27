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
    public GameObject claimPanel;
    public Button claimNFTPassButton;
    public TextMeshProUGUI claimNFTPassButtonText;

    public TextMeshProUGUI bronzeBalanceAmountText;
    public TextMeshProUGUI silverBalanceAmountText;
    public TextMeshProUGUI goldBalanceAmountText;

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

        var contract = ThirdwebManager.Instance.SDK.GetContract("0xDC9E649a41D2aC862b0Ac4bE764FE452079252a7");
        var balance = await contract.ERC721.BalanceOf(Address);
        if (balance == "0")
        {
            claimPanel.SetActive(true);
        }
        else {
            InvokeOnLoggedIn();
        }       
    }

    void InvokeOnLoggedIn() {
        OnLoggedIn?.Invoke(Address);
        GetTokenBalance();
        GetRewardBalance();
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

    public async void ClaimNFTPass() {
        claimNFTPassButtonText.text = "Claiming...";
        claimNFTPassButton.interactable = false;
        var contract = ThirdwebManager.Instance.SDK.GetContract("0xDC9E649a41D2aC862b0Ac4bE764FE452079252a7");
        var result = await contract.ERC721.ClaimTo(Address, 1);
        claimNFTPassButtonText.text = "Claimed NFT Pass!";
        claimPanel.SetActive(false);
        InvokeOnLoggedIn();
    }

    public async void ClaimReward(string _distanceTravelled) {
        var contract = ThirdwebManager.Instance.SDK.GetContract("0x3A10394497717d5B2E6e6334AFa74230e751F4e0");
        if (int.Parse(_distanceTravelled) >= 1000) {
            await contract.ERC1155.ClaimTo(Address, "2", 1);
        } else if (int.Parse(_distanceTravelled) >= 500)
        {
            await contract.ERC1155.ClaimTo(Address, "1", 1);
        }
        else if (int.Parse(_distanceTravelled) >= 300)
        {
            await contract.ERC1155.ClaimTo(Address, "0", 1);
        }
        GetRewardBalance();
    }

    public void ClaimTokenAndReward() {
        ClaimToken();
        ClaimReward(_distanceTravelled);
    }

    public async void GetRewardBalance() {
        var contract = ThirdwebManager.Instance.SDK.GetContract("0x3A10394497717d5B2E6e6334AFa74230e751F4e0");
        var bronzeBalance = await contract.ERC1155.BalanceOf(Address, "0");
        var silverBalance = await contract.ERC1155.BalanceOf(Address, "1");
        var goldBalance = await contract.ERC1155.BalanceOf(Address, "2");

        bronzeBalanceAmountText.text = bronzeBalance;
        silverBalanceAmountText.text = silverBalance;
        goldBalanceAmountText.text = goldBalance;
    }

}
