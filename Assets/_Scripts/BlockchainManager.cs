using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Thirdweb;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using System.Numerics;
using Thirdweb.Contracts.OffersLogic.ContractDefinition;

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

    public GameObject shipLotteryPanel;
    public Button lotteryButton;
    public TextMeshProUGUI lotteryButtonText;
    public Button lotteryOpenButton;
    public TextMeshProUGUI lotteryOpenButtonText;

    public TextMeshProUGUI text_Address_Detail;

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

    public async void Login()
    {
        Address = await ThirdwebManager.Instance.SDK.Wallet.GetAddress();
        text_Address_Detail.text = Address;
        Debug.Log(Address);
        Contract contract = ThirdwebManager.Instance.SDK.GetContract("0xDC9E649a41D2aC862b0Ac4bE764FE452079252a7");
        List<NFT> nftList = await contract.ERC721.GetOwned(Address);
        if (nftList.Count == 0)
        {
            claimPanel.SetActive(true);
        }
        else
        {
            //Show Ship Lottery Panel
            ShowShipLotteryPanel();
        }     
    }

    public void ShowShipLotteryPanel() {
        shipLotteryPanel.SetActive(true);
        lotteryButton.gameObject.SetActive(true);
        lotteryOpenButton.gameObject.SetActive(false);
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
        //Show Ship Lottery Panel
        ShowShipLotteryPanel();
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

        bronzeBalanceAmountText.text = bronzeBalance.ToString();
        silverBalanceAmountText.text = silverBalance.ToString();
        goldBalanceAmountText.text = goldBalance.ToString();
    }

    private int countdownTime = 0;
    public void BuyLotteryTicket()
    {
        ProduceRandomNumber();
        countdownTime = 30;
        lotteryButton.interactable = false;
        lotteryOpenButton.gameObject.SetActive(false);
        //Run count down effect
        StartCoroutine(Countdown());
    }

    IEnumerator Countdown()
    {
        while (countdownTime > 0)
        {
            lotteryButtonText.text = countdownTime.ToString();
            yield return new WaitForSeconds(1f);
            countdownTime--;
        }
        lotteryButtonText.text = "0";
        Debug.Log("Count down end");
        //Show Open Button
        lotteryOpenButton.gameObject.SetActive(true);
        lotteryButton.gameObject.SetActive(false);
        //Open Function
        Debug.Log("Open");
    }

    private async void ProduceRandomNumber()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(
                "0xD0dF3E0Fd752F8391926621Aa6B949c1f0c3Aa17",
                "[{\"type\":\"constructor\",\"name\":\"\",\"inputs\":[{\"type\":\"uint256\",\"name\":\"subscriptionId\",\"internalType\":\"uint256\"}],\"outputs\":[],\"stateMutability\":\"nonpayable\"},{\"type\":\"error\",\"name\":\"OnlyCoordinatorCanFulfill\",\"inputs\":[{\"type\":\"address\",\"name\":\"have\",\"internalType\":\"address\"},{\"type\":\"address\",\"name\":\"want\",\"internalType\":\"address\"}],\"outputs\":[]},{\"type\":\"error\",\"name\":\"OnlyOwnerOrCoordinator\",\"inputs\":[{\"type\":\"address\",\"name\":\"have\",\"internalType\":\"address\"},{\"type\":\"address\",\"name\":\"owner\",\"internalType\":\"address\"},{\"type\":\"address\",\"name\":\"coordinator\",\"internalType\":\"address\"}],\"outputs\":[]},{\"type\":\"error\",\"name\":\"ZeroAddress\",\"inputs\":[],\"outputs\":[]},{\"type\":\"event\",\"name\":\"CoordinatorSet\",\"inputs\":[{\"type\":\"address\",\"name\":\"vrfCoordinator\",\"indexed\":false,\"internalType\":\"address\"}],\"outputs\":[],\"anonymous\":false},{\"type\":\"event\",\"name\":\"OwnershipTransferRequested\",\"inputs\":[{\"type\":\"address\",\"name\":\"from\",\"indexed\":true,\"internalType\":\"address\"},{\"type\":\"address\",\"name\":\"to\",\"indexed\":true,\"internalType\":\"address\"}],\"outputs\":[],\"anonymous\":false},{\"type\":\"event\",\"name\":\"OwnershipTransferred\",\"inputs\":[{\"type\":\"address\",\"name\":\"from\",\"indexed\":true,\"internalType\":\"address\"},{\"type\":\"address\",\"name\":\"to\",\"indexed\":true,\"internalType\":\"address\"}],\"outputs\":[],\"anonymous\":false},{\"type\":\"event\",\"name\":\"RequestFulfilled\",\"inputs\":[{\"type\":\"uint256\",\"name\":\"requestId\",\"indexed\":false,\"internalType\":\"uint256\"},{\"type\":\"uint256[]\",\"name\":\"randomWords\",\"indexed\":false,\"internalType\":\"uint256[]\"}],\"outputs\":[],\"anonymous\":false},{\"type\":\"event\",\"name\":\"RequestSent\",\"inputs\":[{\"type\":\"uint256\",\"name\":\"requestId\",\"indexed\":false,\"internalType\":\"uint256\"},{\"type\":\"uint32\",\"name\":\"numWords\",\"indexed\":false,\"internalType\":\"uint32\"}],\"outputs\":[],\"anonymous\":false},{\"type\":\"function\",\"name\":\"acceptOwnership\",\"inputs\":[],\"outputs\":[],\"stateMutability\":\"nonpayable\"},{\"type\":\"function\",\"name\":\"callbackGasLimit\",\"inputs\":[],\"outputs\":[{\"type\":\"uint32\",\"name\":\"\",\"internalType\":\"uint32\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"getRequestStatus\",\"inputs\":[{\"type\":\"string\",\"name\":\"_requestId\",\"internalType\":\"string\"}],\"outputs\":[{\"type\":\"string\",\"name\":\"firstRandomWord\",\"internalType\":\"string\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"keyHash\",\"inputs\":[],\"outputs\":[{\"type\":\"bytes32\",\"name\":\"\",\"internalType\":\"bytes32\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"lastRequestId\",\"inputs\":[],\"outputs\":[{\"type\":\"uint256\",\"name\":\"\",\"internalType\":\"uint256\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"numWords\",\"inputs\":[],\"outputs\":[{\"type\":\"uint32\",\"name\":\"\",\"internalType\":\"uint32\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"owner\",\"inputs\":[],\"outputs\":[{\"type\":\"address\",\"name\":\"\",\"internalType\":\"address\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"rawFulfillRandomWords\",\"inputs\":[{\"type\":\"uint256\",\"name\":\"requestId\",\"internalType\":\"uint256\"},{\"type\":\"uint256[]\",\"name\":\"randomWords\",\"internalType\":\"uint256[]\"}],\"outputs\":[],\"stateMutability\":\"nonpayable\"},{\"type\":\"function\",\"name\":\"requestConfirmations\",\"inputs\":[],\"outputs\":[{\"type\":\"uint16\",\"name\":\"\",\"internalType\":\"uint16\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"requestIds\",\"inputs\":[{\"type\":\"uint256\",\"name\":\"\",\"internalType\":\"uint256\"}],\"outputs\":[{\"type\":\"uint256\",\"name\":\"\",\"internalType\":\"uint256\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"requestRandomWords\",\"inputs\":[{\"type\":\"bool\",\"name\":\"enableNativePayment\",\"internalType\":\"bool\"}],\"outputs\":[{\"type\":\"uint256\",\"name\":\"requestId\",\"internalType\":\"uint256\"}],\"stateMutability\":\"nonpayable\"},{\"type\":\"function\",\"name\":\"s_requests\",\"inputs\":[{\"type\":\"uint256\",\"name\":\"\",\"internalType\":\"uint256\"}],\"outputs\":[{\"type\":\"bool\",\"name\":\"fulfilled\",\"internalType\":\"bool\"},{\"type\":\"bool\",\"name\":\"exists\",\"internalType\":\"bool\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"s_subscriptionId\",\"inputs\":[],\"outputs\":[{\"type\":\"uint256\",\"name\":\"\",\"internalType\":\"uint256\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"s_vrfCoordinator\",\"inputs\":[],\"outputs\":[{\"type\":\"address\",\"name\":\"\",\"internalType\":\"contractIVRFCoordinatorV2Plus\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"setCoordinator\",\"inputs\":[{\"type\":\"address\",\"name\":\"_vrfCoordinator\",\"internalType\":\"address\"}],\"outputs\":[],\"stateMutability\":\"nonpayable\"},{\"type\":\"function\",\"name\":\"transferOwnership\",\"inputs\":[{\"type\":\"address\",\"name\":\"to\",\"internalType\":\"address\"}],\"outputs\":[],\"stateMutability\":\"nonpayable\"}]"
            );
        await contract.Write("requestRandomWords", (bool)false);

        Debug.Log("ProduceRandomNumber");
    }

    private async void GetRandomNumber()
    {
        var contract = ThirdwebManager.Instance.SDK.GetContract(
                "0xD0dF3E0Fd752F8391926621Aa6B949c1f0c3Aa17",
                "[{\"type\":\"constructor\",\"name\":\"\",\"inputs\":[{\"type\":\"uint256\",\"name\":\"subscriptionId\",\"internalType\":\"uint256\"}],\"outputs\":[],\"stateMutability\":\"nonpayable\"},{\"type\":\"error\",\"name\":\"OnlyCoordinatorCanFulfill\",\"inputs\":[{\"type\":\"address\",\"name\":\"have\",\"internalType\":\"address\"},{\"type\":\"address\",\"name\":\"want\",\"internalType\":\"address\"}],\"outputs\":[]},{\"type\":\"error\",\"name\":\"OnlyOwnerOrCoordinator\",\"inputs\":[{\"type\":\"address\",\"name\":\"have\",\"internalType\":\"address\"},{\"type\":\"address\",\"name\":\"owner\",\"internalType\":\"address\"},{\"type\":\"address\",\"name\":\"coordinator\",\"internalType\":\"address\"}],\"outputs\":[]},{\"type\":\"error\",\"name\":\"ZeroAddress\",\"inputs\":[],\"outputs\":[]},{\"type\":\"event\",\"name\":\"CoordinatorSet\",\"inputs\":[{\"type\":\"address\",\"name\":\"vrfCoordinator\",\"indexed\":false,\"internalType\":\"address\"}],\"outputs\":[],\"anonymous\":false},{\"type\":\"event\",\"name\":\"OwnershipTransferRequested\",\"inputs\":[{\"type\":\"address\",\"name\":\"from\",\"indexed\":true,\"internalType\":\"address\"},{\"type\":\"address\",\"name\":\"to\",\"indexed\":true,\"internalType\":\"address\"}],\"outputs\":[],\"anonymous\":false},{\"type\":\"event\",\"name\":\"OwnershipTransferred\",\"inputs\":[{\"type\":\"address\",\"name\":\"from\",\"indexed\":true,\"internalType\":\"address\"},{\"type\":\"address\",\"name\":\"to\",\"indexed\":true,\"internalType\":\"address\"}],\"outputs\":[],\"anonymous\":false},{\"type\":\"event\",\"name\":\"RequestFulfilled\",\"inputs\":[{\"type\":\"uint256\",\"name\":\"requestId\",\"indexed\":false,\"internalType\":\"uint256\"},{\"type\":\"uint256[]\",\"name\":\"randomWords\",\"indexed\":false,\"internalType\":\"uint256[]\"}],\"outputs\":[],\"anonymous\":false},{\"type\":\"event\",\"name\":\"RequestSent\",\"inputs\":[{\"type\":\"uint256\",\"name\":\"requestId\",\"indexed\":false,\"internalType\":\"uint256\"},{\"type\":\"uint32\",\"name\":\"numWords\",\"indexed\":false,\"internalType\":\"uint32\"}],\"outputs\":[],\"anonymous\":false},{\"type\":\"function\",\"name\":\"acceptOwnership\",\"inputs\":[],\"outputs\":[],\"stateMutability\":\"nonpayable\"},{\"type\":\"function\",\"name\":\"callbackGasLimit\",\"inputs\":[],\"outputs\":[{\"type\":\"uint32\",\"name\":\"\",\"internalType\":\"uint32\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"getRequestStatus\",\"inputs\":[{\"type\":\"string\",\"name\":\"_requestId\",\"internalType\":\"string\"}],\"outputs\":[{\"type\":\"string\",\"name\":\"firstRandomWord\",\"internalType\":\"string\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"keyHash\",\"inputs\":[],\"outputs\":[{\"type\":\"bytes32\",\"name\":\"\",\"internalType\":\"bytes32\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"lastRequestId\",\"inputs\":[],\"outputs\":[{\"type\":\"uint256\",\"name\":\"\",\"internalType\":\"uint256\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"numWords\",\"inputs\":[],\"outputs\":[{\"type\":\"uint32\",\"name\":\"\",\"internalType\":\"uint32\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"owner\",\"inputs\":[],\"outputs\":[{\"type\":\"address\",\"name\":\"\",\"internalType\":\"address\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"rawFulfillRandomWords\",\"inputs\":[{\"type\":\"uint256\",\"name\":\"requestId\",\"internalType\":\"uint256\"},{\"type\":\"uint256[]\",\"name\":\"randomWords\",\"internalType\":\"uint256[]\"}],\"outputs\":[],\"stateMutability\":\"nonpayable\"},{\"type\":\"function\",\"name\":\"requestConfirmations\",\"inputs\":[],\"outputs\":[{\"type\":\"uint16\",\"name\":\"\",\"internalType\":\"uint16\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"requestIds\",\"inputs\":[{\"type\":\"uint256\",\"name\":\"\",\"internalType\":\"uint256\"}],\"outputs\":[{\"type\":\"uint256\",\"name\":\"\",\"internalType\":\"uint256\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"requestRandomWords\",\"inputs\":[{\"type\":\"bool\",\"name\":\"enableNativePayment\",\"internalType\":\"bool\"}],\"outputs\":[{\"type\":\"uint256\",\"name\":\"requestId\",\"internalType\":\"uint256\"}],\"stateMutability\":\"nonpayable\"},{\"type\":\"function\",\"name\":\"s_requests\",\"inputs\":[{\"type\":\"uint256\",\"name\":\"\",\"internalType\":\"uint256\"}],\"outputs\":[{\"type\":\"bool\",\"name\":\"fulfilled\",\"internalType\":\"bool\"},{\"type\":\"bool\",\"name\":\"exists\",\"internalType\":\"bool\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"s_subscriptionId\",\"inputs\":[],\"outputs\":[{\"type\":\"uint256\",\"name\":\"\",\"internalType\":\"uint256\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"s_vrfCoordinator\",\"inputs\":[],\"outputs\":[{\"type\":\"address\",\"name\":\"\",\"internalType\":\"contractIVRFCoordinatorV2Plus\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"setCoordinator\",\"inputs\":[{\"type\":\"address\",\"name\":\"_vrfCoordinator\",\"internalType\":\"address\"}],\"outputs\":[],\"stateMutability\":\"nonpayable\"},{\"type\":\"function\",\"name\":\"transferOwnership\",\"inputs\":[{\"type\":\"address\",\"name\":\"to\",\"internalType\":\"address\"}],\"outputs\":[],\"stateMutability\":\"nonpayable\"}]"
            );
        BigInteger latestRequestId = await contract.Read<BigInteger>("lastRequestId");
        Debug.Log("latestRequestId" + latestRequestId);
        string latestRequestIdString = latestRequestId.ToString();
        Debug.Log("latestRequestIdString" + latestRequestIdString);
        string result = await contract.Read<string>("getRequestStatus", latestRequestIdString);
        Debug.Log("result" + result);
        EvaluateString(result);
    }

    public void EvaluateString(string input)
    {
        if (int.TryParse(input, out int result))
        {
            switch (result)
            {
                case 1:
                    Gold2SpaceShip();
                    break;
                case 2:
                case 3:
                    Gold1SpaceShip();
                    break;
                case 4:
                case 5:
                    Silver2SpaceShip();
                    break;
                case 6:
                case 7:
                    Silver1SpaceShip();
                    break;
                case 8:
                case 9:
                    DefaultSpaceShip();
                    break;
                default:
                    DefaultSpaceShip();
                    break;
            }
        }
        ResetSpaceShipLotteryPanel();
        InvokeOnLoggedIn();
    }

    public void claim()
    {
        GetRandomNumber();
    }

    public GameObject defaultSpaceShip;
    public GameObject silver1SpaceShip;
    public GameObject silver2SpaceShip;
    public GameObject gold1SpaceShip;
    public GameObject gold2SpaceShip;

    private void changeCharacterSpeed(float characterSpeed) {
        GameObject character = GameObject.Find("Character");
        if (character != null)
        {
            CharacterManager characterManager = character.GetComponent<CharacterManager>();
            if (characterManager != null)
            {
                characterManager._forwardSpeed = characterSpeed;
            }
            else
            {
                Debug.LogError("No characterManager");
            }
        }
        else
        {
            Debug.LogError("No character");
        }
    }

    private void UpdateCamerad(int offset)
    {
        GameObject mainCamera = GameObject.Find("Main Camera");
        if (mainCamera != null)
        {
            CameraController cameraController = mainCamera.GetComponent<CameraController>();
            if (cameraController != null)
            {
                cameraController._offset = new UnityEngine.Vector3(0, 2, offset);
            }
            else
            {
                Debug.LogError("No characterManager");
            }
        }
        else
        {
            Debug.LogError("No character");
        }
    }

    private void DefaultSpaceShip()
    {
        defaultSpaceShip.SetActive(true);
        silver1SpaceShip.SetActive(false);
        silver2SpaceShip.SetActive(false);
        gold1SpaceShip.SetActive(false);
        gold2SpaceShip.SetActive(false);
        changeCharacterSpeed(50);
        UpdateCamerad(0);
    }

    private void Silver1SpaceShip()
    {
        defaultSpaceShip.SetActive(false);
        silver1SpaceShip.SetActive(true);
        silver2SpaceShip.SetActive(false);
        gold1SpaceShip.SetActive(false);
        gold2SpaceShip.SetActive(false);
        changeCharacterSpeed(80);
        UpdateCamerad(2);
    }

    private void Silver2SpaceShip()
    {
        defaultSpaceShip.SetActive(false);
        silver1SpaceShip.SetActive(false);
        silver2SpaceShip.SetActive(true);
        gold1SpaceShip.SetActive(false);
        gold2SpaceShip.SetActive(false);
        changeCharacterSpeed(120);
        UpdateCamerad(5);
    }

    private void Gold1SpaceShip()
    {
        defaultSpaceShip.SetActive(false);
        silver1SpaceShip.SetActive(false);
        silver2SpaceShip.SetActive(false);
        gold1SpaceShip.SetActive(true);
        gold2SpaceShip.SetActive(false);
        changeCharacterSpeed(150);
        UpdateCamerad(9);
    }

    private void Gold2SpaceShip()
    {
        defaultSpaceShip.SetActive(false);
        silver1SpaceShip.SetActive(false);
        silver2SpaceShip.SetActive(false);
        gold1SpaceShip.SetActive(false);
        gold2SpaceShip.SetActive(true);
        changeCharacterSpeed(200);
        UpdateCamerad(14);
    }

    private void ResetSpaceShipLotteryPanel() {
        lotteryOpenButton.gameObject.SetActive(false);
        lotteryButton.gameObject.SetActive(true);
        lotteryButton.interactable = true;
        lotteryButtonText.text = "Lottery";
        shipLotteryPanel.SetActive(false);
    }
}
