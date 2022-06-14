% fir fourier series to some data. Compare fits to permutation.

% first plot objective resultL

%separate options.


testtype=3;
usebin=1;
switch testtype
    case 1
%test RT relative to target onset:
dataIN = GFX_TargPosData;
typeOnset = 'Target';
typeDV = 'RT';
    case 2
%%
%test RT relative to response onset:
dataIN = GFX_RespPosData;
typeOnset = 'Response';
typeDV = 'RT';
%%
    case 3
        %test ACC relative to target onset.
        
        dataIN = GFX_TargPosData;
        typeOnset = 'Target';
        typeDV = 'Accuracy';
        
    case 4
        % Test response (click) likelihood, relative to resp onset.
        dataIN = GFX_RespPosData;
        typeOnset = 'Response';
        typeDV='Counts';
        
end
%%
cfg=[];
cfg.subjIDs = subjIDs;
cfg.type = typeOnset;
cfg.DV = typeDV;
cfg.datadir= datadir; % for orienting to figures folder
cfg.HeadData= GFX_headY;
cfg.pidx1= pidx1;
cfg.pidx2= pidx2;
cfg.plotlevel = 'GFX'; % plot separate figures per participant
cfg.norm=0; % already z scored, so don't tweak.
cfg.ylims = [-.15 .15]; % if norm =0;
cfg.normtype= 'relative';
%%

%just one gait at a time (its a slow process).
nGaits_toPlot=1; %:2

% plot_FourierFit(cfg,dataIN);
%%
GFX_headY = cfg.HeadData;
usecols = {[0 .7 0], [.7 0 0], [.7 0 .7]}; % R Gr, Prp

figure(1); clf; set(gcf, 'color', 'w', 'units', 'normalized', 'position', [0 0 .75 1]);
nsubs = length(cfg.subjIDs);


pc=1; % plot counter
pspots = [1,3,5,2,4,6]; %suplot order
psubj= 'GFX'; % print ppid.
% both this and the next use the same figure function:

iLR=3; % use combined data (not sep feet)
gaitfield = {'gc', 'doubgc'};
binfield = {'','_binned'};
    
    legp=[]; % for legend
    ppantData=[];
    plotHead=[];
    shuffData=[];
    
    %which field of datastructure to plot?
    if strcmp(cfg.DV, 'RT')
        usefield = [gaitfield{nGaits_toPlot} binfield{usebin+1} '_rts'];        
        ylabis = 'z(RT)';
    elseif strcmp(cfg.DV, 'Accuracy')
        usefield = [gaitfield{nGaits_toPlot} '_binned_Acc'];
         if ~cfg.norm
            ylabis=  cfg.DV;
        else
            ylabis = [cfg.DV 'norm: ' cfg.normtype];
         end
    elseif strcmp(cfg.DV, 'Counts')
        usefield = [gaitfield{nGaits_toPlot} '_binned_counts'];
        ylabis = [cfg.type ' ' cfg.DV];
    end
    
    %collate data:
    for isub= 1:size(dataIN,1)
        
        ppantData(isub,:)= dataIN(isub,iLR).(usefield);
        shuffData(isub,:,:) = dataIN(isub,iLR).([usefield '_shuff']);
        plotHead(isub,:) = GFX_headY(isub).(gaitfield{nGaits_toPlot});
    end
    %% if normON , normalize as appropriate
    
    if cfg.norm==1
        pM = nanmean(ppantData,2);
        meanVals= repmat(pM, 1, size(ppantData,2));
        
        
        if strcmp(cfg.normtype, 'absolute')
            data = ppantData - meanVals;
        elseif strcmp(cfg.normtype, 'relative')
            data = ppantData  ./ meanVals;
            data=data-1;
        elseif strcmp(cfg.normtype, 'relchange')
            data = (ppantData  - meanVals) ./ meanVals;
        elseif strcmp(cfg.normtype, 'normchange')
            data = (ppantData  - meanVals) ./ (ppantData + meanVals);
        elseif strcmp(cfg.normtype, 'db')
            data = 10*log10(ppantData  ./ meanVals);
        end
        
        ppantData= data;        
    end
    %% other specs:
    if nGaits_toPlot==1
        
        pidx= cfg.pidx1;
        ftnames= {'LR', 'RL', 'combined'};
    else
        pidx= cfg.pidx2;
        ftnames= {'LRL', 'RLR', 'combined'};
    end
    
    %note that pidx is adjusted to all datapoints, if not using the  bin.
    if usebin==0
        pidx=1:size(ppantData,2);
    end
    
    %x axis:          %approx centre point of the binns.
            mdiff = round(mean(diff(pidx)./2));
            xvec = pidx(1:end-1) + mdiff;
    
    %% extract fourier fits per shuffled series:
    % Declaring the type of fit.
    FitType = 'fourier1';
    % Creating and showing a table array to specify bounds.
    CoeffNames = coeffnames(fittype(FitType));
%%
%set bounds for w
CoeffBounds = array2table([-Inf(1,length(CoeffNames));...
    Inf(1,length(CoeffNames))],'RowNames',...
    ["lower bound", "upper bound"],'VariableNames',CoeffNames);
%%
% Specifying bounds according to the position shown by the table.
% e.g. to force fit with w ~ 1.545, we ChangeBound of w parameter
% CoeffBounds.w(1) = 1.54;
% CoeffBounds.w(2) = 1.55;
%
Hzspace = [0.01:.2:6];
% perW=per
    fits_Rsquared_obsrvd = nan(1, length(Hzspace));
    fits_Rsquared_shuff = nan(size(shuffData,2), length(Hzspace));
    fits_Rsquared_shuffCV = nan(3, length(Hzspace));
    meanShuff = squeeze(mean(shuffData,1));
    gM=squeeze(nanmean(ppantData));
    
    %best fit (use model params below):
    f = fit(xvec', gM', 'fourier1'); %unbounded
    
    % step through w, forcing fit at particular periods, by updating the
    % bounds in fit options.
    for ifreq= 1:length(Hzspace)
         % include period and Rsquared
            %treat max xvec as our full 'period'
%             Hzapp = xvec(end)/ (2*pi/(f.w));
        testw = 2*pi*Hzspace(ifreq)/xvec(end); 
        
        CoeffBounds.w(1) = testw;
        CoeffBounds.w(2) = testw;
        
        %update fit opts settings
        
        %Update Fit Options setting.
        FitOpts = fitoptions('Method','NonlinearLeastSquares','Lower',table2array(CoeffBounds(1,:)),...
            'Upper',table2array(CoeffBounds(2,:)));

    %first test this period on observed data
    %set last coefficient value in the fourier model (w) to this value:
    
    [tmpf,gof] = fit(xvec', gM', 'fourier1', FitOpts);
    % how good/bad was the fit?
    fits_Rsquared_obsrvd(1,ifreq) = gof.rsquare;

    % for each shuffle as well, calc the fits.
    for iperm = 1:size(meanShuff)
    [tmpf,gof] = fit(xvec', squeeze(meanShuff(iperm,:))', 'fourier1', FitOpts);    
    fits_Rsquared_shuff(iperm,ifreq) = gof.rsquare;    
    end % per freq
    
    % per freq, store the 95%CI. of Rsq values(plotted below)
    fits_Rsquared_shuffCV(:,ifreq) = quantile(fits_Rsquared_shuff(:,ifreq), [.05, .5, .95]);
    disp(['fin perm for freq ' num2str(ifreq) ' / ' num2str(length(Hzspace))]);
    end
    
    %% %%%%%%%%%%%%%%%%%%% first row of plot. 
    % Mean data, with errorbars head pos overlayed.
    subplot(3,2,nGaits_toPlot)
    hold on;

    gM = squeeze(mean(ppantData));
    stE = CousineauSEM(ppantData);
    
    % finely sampled bar, each gait "%" point.
    bh=bar(xvec, gM);
    hold on;
    errorbar(xvec, gM, stE, ...
        'color', 'k',...
        'linestyle', 'none',...
        'linew', 2);
    bh.FaceColor = usecols{iLR};
    legp(iLR)= bh;
    
    ylabel(ylabis)
    
   %adjust ylims to capture 2SD*range of data    
   sdrange = max(gM) - min(gM);
   ylim([min(gM)-.5*sdrange max(gM)+1*sdrange])
    %%
    % add head pos
    hold on
    yyaxis right
    stEH= CousineauSEM(plotHead);
    pH= nanmean(plotHead,1);
   sh= shadedErrorBar(1:size(plotHead,2), pH, stEH,'b',1) ; 
   sh.mainLine.LineStyle='-';
    legp = sh.mainLine;
    set(gca,'ytick', []);
    lg=legend(legp, 'Head position (vertical)', 'location', 'NorthEast', 'fontsize', 10);
   %% lengthen yyaxis right so legend doesnt obscure
    ytop = max(pH);
    ylt= get(gca,'ylim');
    sdrange = max(pH) - min(pH);
   ylim([min(pH) 1.2*max(pH)])
   
    %%
    title([psubj '  ' ftnames{iLR} ' N=' num2str(nsubs)], 'interpreter', 'none');
    midp=xvec(ceil(length(xvec)/2));
    set(gca,'fontsize', 15, 'xtick', [1, midp, xvec(end)], 'XTickLabels', {'0', '50', '100%'})   
    xlabel([ cfg.type ' onset as % of gait-cycle ']);%
 
    
    %% %%%%%%%%%%%%%%%%%%% second row of plot. 
    % Mean data, now with fourier fit overlayed. 
    %% also prepare next overlay:
    subplot(3,2,nGaits_toPlot+2);   
    % finely sampled bar, each gait "%" point.
    bh=bar(xvec, gM);
    hold on;
    errorbar(xvec, gM, stE, ...
        'color', 'k',...
        'linestyle', 'none',...
        'linew', 2);
    bh.FaceColor = usecols{iLR};
     sdrange = max(gM) - min(gM);
   ylim([min(gM)-.5*sdrange max(gM)+1*sdrange])
    %%
    % perform fourier fit:
    % fit all periods, from .01 to .
    %              for ifreq -
   
    [f,gof]= fit(xvec',gM',  'fourier1');
    
    % plot:
    hold on;
    %             yyaxis right
    h=plot(f, xvec, gM);%,
    h(2).LineWidth = 2;
    h(2).Color = 'k';
    %%
    %treat max xvec as our full 'period'
    fitperiod = f.w;
    %convert to period per samples.
    % include period and Rsquared
    %treat max xvec as our full 'period'
    Hzapp = xvec(end)/ (2*pi/(f.w));
    legdetails = [sprintf('%.2f', Hzapp) ' Hz_G_C, R^2 = ' sprintf('%.2f', gof.rsquare) ];
    legend(h(2), legdetails, 'fontsize', 15, 'autoupdate', 'off')
    
    ylabel(ylabis)
    set(gca,'fontsize', 15, 'xtick', [1, midp, xvec(end)], 'XTickLabels', {'0', '50', '100%'})
    xlabel([ cfg.type ' onset as % of gait-cycle ']);%
    %%
    %             set(gca,
    
    %
    
    subplot(3,2,nGaits_toPlot+4);
    
    pO=plot(Hzspace, fits_Rsquared_obsrvd, 'k', 'linew', 3);
    title('(forced) Fits per frequency')
    ylabel('R^2');
    xlabel('Frequency (Hz)')
    
    hold on;
    
    %
%     plot(Hzspace, fits_Rsquared_shuffCV(1,:), ':-', 'linew', 2, 'color', [.8 .8 .8] );
    ph=plot(Hzspace, fits_Rsquared_shuffCV(3,:), ':', 'linew', 2, 'color', 'k');
    legend([pO, ph],{'observed', '95% CI shuffled data'})
     set(gca,'fontsize', 15)
    %%
 


%%
cd([cfg.datadir filesep  'Figures' filesep  cfg.type ' RT binned'])

% print([psubj ' ' cfg.type ' onset ' cfg.DV ' binned norm' num2str(cfg.norm)],'-dpng');
